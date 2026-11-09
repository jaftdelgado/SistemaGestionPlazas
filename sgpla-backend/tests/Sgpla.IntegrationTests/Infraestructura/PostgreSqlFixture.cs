using Npgsql;
using Sgpla.Database;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(Sgpla.IntegrationTests.Infraestructura.PostgreSqlFixture))]

namespace Sgpla.IntegrationTests.Infraestructura;

/// <summary>
/// Levanta un PostgreSQL en contenedor una sola vez por ejecución de pruebas
/// y aplica las migraciones de DbUp sobre la base <c>sgpla-bd</c>.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private const string NombreBaseDatos = "sgpla-bd";

    public const string ClavePeriodoActual = "999800";

    public const string ClavePeriodoSiguiente = "999801";

    private readonly PostgreSqlContainer _contenedor = new PostgreSqlBuilder("postgres:18.6-alpine").Build();

    public string CadenaConexion { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        await _contenedor.StartAsync();

        CadenaConexion = new NpgsqlConnectionStringBuilder(_contenedor.GetConnectionString())
        {
            Database = NombreBaseDatos,
        }.ConnectionString;

        var resultado = DatabaseMigrator.Migrar(CadenaConexion, crearBaseSiNoExiste: true);
        if (!resultado.Successful)
        {
            throw new InvalidOperationException("No se pudieron aplicar las migraciones.", resultado.Error);
        }

        await using var conexion = new NpgsqlConnection(CadenaConexion);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            INSERT INTO academico.periodo_escolar (clave, fecha_inicio, fecha_fin)
            VALUES ('999800', '2026-02-02', '2026-07-10'),
                   ('999801', '2026-08-10', '2027-01-22')
            ON CONFLICT (clave) DO NOTHING;
            """;
        await comando.ExecuteNonQueryAsync();
    }

    public ValueTask DisposeAsync() => _contenedor.DisposeAsync();
}
