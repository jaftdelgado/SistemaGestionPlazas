using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests.Institucional;

/// <summary>Regiones de solo lectura: los valores los carga la semilla (Modulo_Institucional.md §6, D1).</summary>
public sealed class RegionEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private const string Ruta = "/api/v1/institucional/regiones";

    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Listar_DevuelveLasCincoRegionesEnOrdenDeClave()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri(), Cancelacion);
        var regiones = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        regiones.ValueKind.ShouldBe(JsonValueKind.Array);
        regiones.EnumerateArray()
            .Select(r => (r.GetProperty("id").GetInt32(), r.GetProperty("clave").GetInt32(), r.GetProperty("nombre").GetString()))
            .ShouldBe(
            [
                (1, 1, "Xalapa"),
                (2, 2, "Veracruz"),
                (3, 3, "Orizaba-Córdoba"),
                (4, 4, "Poza Rica-Tuxpan"),
                (5, 5, "Coatzacoalcos-Minatitlán"),
            ]);
    }

    [Fact]
    public async Task Obtener_Existente_Responde200()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri("/3"), Cancelacion);
        var region = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        region.GetProperty("id").GetInt32().ShouldBe(3);
        region.GetProperty("clave").GetInt32().ShouldBe(3);
        region.GetProperty("nombre").GetString().ShouldBe("Orizaba-Córdoba");
    }

    [Fact]
    public async Task Obtener_Inexistente_Responde404ConCodigo()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri("/999"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problema.GetProperty("codigo").GetString().ShouldBe("Region.NoEncontrado");
    }

    [Theory]
    [InlineData("POST", "")]
    [InlineData("PUT", "/1")]
    [InlineData("DELETE", "/1")]
    public async Task Escritura_NoExiste_Responde405(string metodo, string sufijo)
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        using var solicitud = new HttpRequestMessage(new HttpMethod(metodo), Uri(sufijo))
        {
            Content = JsonContent.Create(new { clave = 6, nombre = "Región nueva" }),
        };

        using var respuesta = await cliente.SendAsync(solicitud, Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static Uri Uri(string sufijo = "") => new($"{Ruta}{sufijo}", UriKind.Relative);

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>(Cancelacion);
}
