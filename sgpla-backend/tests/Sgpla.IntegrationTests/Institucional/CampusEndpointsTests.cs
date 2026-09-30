using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests.Institucional;

/// <summary>Campus de solo lectura: los valores los carga la semilla.</summary>
public sealed class CampusEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private const string Ruta = "/api/v1/institucional/campus";

    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Listar_DevuelveLos24CampusEnOrdenDeClave()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri(), Cancelacion);
        var campus = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        campus.ValueKind.ShouldBe(JsonValueKind.Array);
        campus.GetArrayLength().ShouldBe(24);
        campus[0].GetProperty("clave").GetString().ShouldBe("A");
        campus[0].GetProperty("nombre").GetString().ShouldBe("Acayucan");
    }

    [Fact]
    public async Task Listar_ConRegionId_DevuelveSoloLosDeEsaRegion()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri("?regionId=2"), Cancelacion);
        var campus = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var elementos = campus.EnumerateArray().ToList();
        elementos.Select(c => c.GetProperty("clave").GetString()).ShouldBe(["B", "V"]);
        elementos.All(c => c.GetProperty("region").GetProperty("clave").GetInt32() == 2).ShouldBeTrue();
    }

    [Fact]
    public async Task Listar_ConRegionInexistente_DevuelveArregloVacio()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri("?regionId=99"), Cancelacion);
        var campus = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        campus.GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Listar_ConRegionIdNoPositivo_Responde400ConErrorEnRegionId()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri("?regionId=0"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty("regionId", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Obtener_Existente_Responde200ConLaRegionAnidada()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri("/1"), Cancelacion);
        var campus = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        campus.GetProperty("id").GetInt32().ShouldBe(1);
        campus.GetProperty("clave").GetString().ShouldBe("X");
        campus.GetProperty("nombre").GetString().ShouldBe("Xalapa");
        campus.GetProperty("region").GetProperty("nombre").GetString().ShouldBe("Xalapa");
    }

    [Fact]
    public async Task Obtener_Inexistente_Responde404ConCodigo()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri("/999"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problema.GetProperty("codigo").GetString().ShouldBe("Campus.NoEncontrado");
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
            Content = JsonContent.Create(new { clave = "Y", nombre = "Campus nuevo", regionId = 1 }),
        };

        using var respuesta = await cliente.SendAsync(solicitud, Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static Uri Uri(string sufijo = "") => new($"{Ruta}{sufijo}", UriKind.Relative);

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>(Cancelacion);
}
