using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests.Catalogos;

/// <summary>
/// Catálogo fijo: los 212 municipios de Veracruz, con el id de la clave municipal del INEGI (DATABASE.md §6.4).
/// Paginado y con búsqueda por nombre, porque el frontend lo usa en un Select.
/// </summary>
public sealed class MunicipioEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private const string Ruta = "/api/v1/catalogos/municipios";

    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Listar_SinParametros_Devuelve212EnOrdenAlfabetico()
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.GetAsync(Uri(), Cancelacion);
        var pagina = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        pagina.GetProperty("total").GetInt32().ShouldBe(212);
        pagina.GetProperty("pagina").GetInt32().ShouldBe(1);
        pagina.GetProperty("tamanoPagina").GetInt32().ShouldBe(20);

        var elementos = pagina.GetProperty("elementos");
        elementos.GetArrayLength().ShouldBe(20);
        elementos[0].GetProperty("nombre").GetString().ShouldBe("Acajete");
    }

    [Theory]
    [InlineData("xalapa")]
    [InlineData("XALAPA")]
    [InlineData("xálapa")]
    public async Task Listar_ConBusqueda_IgnoraMayusculasYAcentos(string busqueda)
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.GetAsync(Uri($"?busqueda={System.Uri.EscapeDataString(busqueda)}"), Cancelacion);
        var pagina = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        pagina.GetProperty("total").GetInt32().ShouldBe(1);
        var elementos = pagina.GetProperty("elementos");
        elementos.GetArrayLength().ShouldBe(1);
        elementos[0].GetProperty("id").GetInt32().ShouldBe(87);
        elementos[0].GetProperty("nombre").GetString().ShouldBe("Xalapa");
    }

    [Fact]
    public async Task Listar_ConBusquedaSinCoincidencias_DevuelveTotalCero()
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.GetAsync(Uri("?busqueda=zzzznoexiste"), Cancelacion);
        var pagina = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        pagina.GetProperty("total").GetInt32().ShouldBe(0);
        pagina.GetProperty("elementos").GetArrayLength().ShouldBe(0);
    }

    [Theory]
    [InlineData("?pagina=0")]
    [InlineData("?tamanoPagina=101")]
    public async Task Listar_ConPaginacionInvalida_Responde400(string queryString)
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.GetAsync(Uri(queryString), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Listar_ConBusquedaDemasiadoLarga_Responde400ConErrorEnBusqueda()
    {
        using var cliente = _api.CreateClient();
        var busqueda = new string('a', 151);

        using var respuesta = await cliente.GetAsync(Uri($"?busqueda={busqueda}"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty("busqueda", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Listar_ConBusquedaDe150CaracteresTrasRecortar_Responde200()
    {
        using var cliente = _api.CreateClient();
        var busqueda = $" {new string('a', 150)} ";

        using var respuesta = await cliente.GetAsync(
            Uri($"?busqueda={System.Uri.EscapeDataString(busqueda)}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Listar_ConEspaciosRepetidosEnBusqueda_EncuentraCoincidencia()
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.GetAsync(
            Uri($"?busqueda={System.Uri.EscapeDataString("poza   rica")}"), Cancelacion);
        var pagina = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        pagina.GetProperty("total").GetInt32().ShouldBe(1);
        pagina.GetProperty("elementos")[0].GetProperty("nombre").GetString().ShouldBe("Poza Rica De Hidalgo");
    }

    [Fact]
    public async Task Obtener_Existente_Responde200()
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.GetAsync(Uri("/87"), Cancelacion);
        var municipio = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        municipio.GetProperty("id").GetInt32().ShouldBe(87);
        municipio.GetProperty("nombre").GetString().ShouldBe("Xalapa");
    }

    [Fact]
    public async Task Obtener_Inexistente_Responde404ConCodigo()
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.GetAsync(Uri("/999"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problema.GetProperty("codigo").GetString().ShouldBe("Municipio.NoEncontrado");
    }

    [Theory]
    [InlineData("POST", "")]
    [InlineData("PUT", "/1")]
    [InlineData("DELETE", "/1")]
    public async Task Escritura_NoExiste_Responde405(string metodo, string sufijo)
    {
        using var cliente = _api.CreateClient();
        using var solicitud = new HttpRequestMessage(new HttpMethod(metodo), Uri(sufijo))
        {
            Content = JsonContent.Create(new { nombre = "Municipio nuevo" }),
        };

        using var respuesta = await cliente.SendAsync(solicitud, Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static Uri Uri(string sufijo = "") => new($"{Ruta}{sufijo}", UriKind.Relative);

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>(Cancelacion);
}
