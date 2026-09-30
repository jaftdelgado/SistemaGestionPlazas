using System.Net;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests;

public sealed class EsqueletoTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private readonly SgplaApiFactory _api = new(sqlServer);

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
        await using var conexion = new SqlConnection(sqlServer.CadenaConexion);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new SqlCommand(
            "SELECT name FROM sys.schemas WHERE name IN (N'academico', N'plazas', N'usuarios', N'integracion')",
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
        await using var conexion = new SqlConnection(sqlServer.CadenaConexion);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new SqlCommand(
            """
            SELECT s.name, COUNT(*)
            FROM sys.tables AS t
            JOIN sys.schemas AS s ON s.schema_id = t.schema_id
            WHERE s.name IN (N'academico', N'plazas', N'usuarios', N'integracion')
            GROUP BY s.name
            """,
            conexion);

        var tablasPorEsquema = new Dictionary<string, int>();
        await using var lector = await comando.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await lector.ReadAsync(TestContext.Current.CancellationToken))
        {
            tablasPorEsquema[lector.GetString(0)] = lector.GetInt32(1);
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
        await using var conexion = new SqlConnection(sqlServer.CadenaConexion);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new SqlCommand(
            """
            SELECT t.name + N'.' + c.name
            FROM sys.columns AS c
            JOIN sys.tables AS t ON t.object_id = c.object_id
            JOIN sys.schemas AS s ON s.schema_id = t.schema_id
            WHERE s.name = N'academico'
              AND ((t.name = N'plan_estudios' AND c.name = N'archivo_plan_estudios_id')
                OR (t.name IN (N'sistema_educativo', N'nivel_formacion', N'area_formacion') AND c.name = N'fecha_eliminacion'))
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
