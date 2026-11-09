using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests;

public sealed class EsqueletoTests(PostgreSqlFixture baseDatos) : IAsyncDisposable
{
    private readonly SgplaApiFactory _api = new(baseDatos);

    [Fact]
    public async Task Health_ConBaseDeDatosDisponible_Responde200()
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Migraciones_CreanLosCuatroEsquemas()
    {
        await using var conexion = new NpgsqlConnection(baseDatos.CadenaConexion);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new NpgsqlCommand(
            "SELECT schema_name FROM information_schema.schemata WHERE schema_name IN ('academico', 'plazas', 'usuarios', 'integracion')",
            conexion);

        var esquemas = new List<string>();
        await using var lector = await comando.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await lector.ReadAsync(TestContext.Current.CancellationToken))
        {
            esquemas.Add(lector.GetString(0));
        }

        esquemas.ShouldBe(["academico", "integracion", "plazas", "usuarios"], ignoreOrder: true);
    }

    [Fact]
    public async Task Migraciones_CreanLasTablasDeLaBaseInicial()
    {
        await using var conexion = new NpgsqlConnection(baseDatos.CadenaConexion);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new NpgsqlCommand(
            """
            SELECT table_schema, COUNT(*)
            FROM information_schema.tables
            WHERE table_type = 'BASE TABLE'
              AND table_schema IN ('academico', 'plazas', 'usuarios', 'integracion')
            GROUP BY table_schema
            """,
            conexion);

        var tablasPorEsquema = new Dictionary<string, int>();
        await using var lector = await comando.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await lector.ReadAsync(TestContext.Current.CancellationToken))
        {
            tablasPorEsquema[lector.GetString(0)] = (int)lector.GetInt64(1);
        }

        tablasPorEsquema.ShouldBe(new Dictionary<string, int>
        {
            ["academico"] = 23,
            ["integracion"] = 1,
            ["plazas"] = 25,
            ["usuarios"] = 5,
        }, ignoreOrder: true);
    }

    [Fact]
    public async Task Migraciones_NoCreanArchivoDelPlanNiBajaEnClasificaciones()
    {
        // El plan no tiene archivo y las tres clasificaciones no tienen baja lógica.
        await using var conexion = new NpgsqlConnection(baseDatos.CadenaConexion);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new NpgsqlCommand(
            """
            SELECT table_name || '.' || column_name
            FROM information_schema.columns
            WHERE table_schema = 'academico'
              AND ((table_name = 'plan_estudios' AND column_name = 'archivo_plan_estudios_id')
                OR (table_name IN ('sistema_educativo', 'nivel_formacion', 'area_formacion') AND column_name = 'fecha_eliminacion'))
            """,
            conexion);

        var columnas = new List<string>();
        await using var lector = await comando.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await lector.ReadAsync(TestContext.Current.CancellationToken))
        {
            columnas.Add(lector.GetString(0));
        }

        columnas.ShouldBeEmpty();
    }

    [Fact]
    public async Task Migraciones_SolicitudAperturaSinVinculacionNiColumnaVersion()
    {
        await using var conexion = new NpgsqlConnection(baseDatos.CadenaConexion);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new NpgsqlCommand(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'academico'
              AND table_name = 'solicitud_apertura'
              AND column_name IN ('programacion_academica_id', 'vinculada_en', 'vinculada_por_usuario_id', 'version')
            """,
            conexion);

        // La concurrencia optimista usa la columna de sistema xmin, así que la tabla no define una columna version.
        var columnas = new List<string>();
        await using var lector = await comando.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await lector.ReadAsync(TestContext.Current.CancellationToken))
        {
            columnas.Add(lector.GetString(0));
        }

        columnas.ShouldBeEmpty();
    }

    [Fact]
    public void Persistencia_NoRegistraValoresDeParametros()
    {
        // EnableSensitiveDataLogging nunca se activa.
        using var alcance = _api.Services.CreateScope();
        var opciones = alcance.ServiceProvider.GetRequiredService<DbContextOptions<SgplaDbContext>>();

        var registraDatosSensibles = opciones.FindExtension<CoreOptionsExtension>()?.IsSensitiveDataLoggingEnabled ?? false;

        registraDatosSensibles.ShouldBeFalse();
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();
}
