using Microsoft.Data.SqlClient;
using Sgpla.Database;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(Sgpla.IntegrationTests.Infraestructura.SqlServerFixture))]

namespace Sgpla.IntegrationTests.Infraestructura;

/// <summary>
/// Levanta un SQL Server en contenedor una sola vez por ejecución de pruebas
/// y aplica las migraciones de DbUp sobre la base <c>sgpla-bd</c>.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string NombreBaseDatos = "sgpla-bd";

    public const string ClavePeriodoActual = "999800";

    public const string ClavePeriodoSiguiente = "999801";

    private readonly MsSqlContainer _contenedor = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public string CadenaConexion { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        await _contenedor.StartAsync();

        CadenaConexion = new SqlConnectionStringBuilder(_contenedor.GetConnectionString())
        {
            InitialCatalog = NombreBaseDatos,
        }.ConnectionString;

        var resultado = DatabaseMigrator.Migrar(CadenaConexion, crearBaseSiNoExiste: true);
        if (!resultado.Successful)
        {
            throw new InvalidOperationException("No se pudieron aplicar las migraciones.", resultado.Error);
        }

        await using var conexion = new SqlConnection(CadenaConexion);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            IF NOT EXISTS (SELECT 1 FROM academico.periodo_escolar WHERE clave = '999800')
                INSERT INTO academico.periodo_escolar (clave, fecha_inicio, fecha_fin)
                VALUES ('999800', '2026-02-02', '2026-07-10');
            IF NOT EXISTS (SELECT 1 FROM academico.periodo_escolar WHERE clave = '999801')
                INSERT INTO academico.periodo_escolar (clave, fecha_inicio, fecha_fin)
                VALUES ('999801', '2026-08-10', '2027-01-22');
            """;
        await comando.ExecuteNonQueryAsync();
    }

    public ValueTask DisposeAsync() => _contenedor.DisposeAsync();
}
