using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests.Catalogos;

/// <summary>
/// Sistemas educativos, niveles y áreas de formación (Modulo_OfertaEducativa.md, sección 5): catálogos fijos de solo
/// lectura con los valores de la semilla.
/// </summary>
public sealed class ClasificacionesAcademicasEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    public static TheoryData<string, string> Catalogos() => new()
    {
        { "sistemas-educativos", "SistemaEducativo" },
        { "niveles-formacion", "NivelFormacion" },
        { "areas-formacion", "AreaFormacion" },
    };

    [Fact]
    public async Task Listar_SistemasEducativos_DevuelveLosSeisEnOrdenDeId()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri("sistemas-educativos"), Cancelacion);
        var elementos = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        elementos.ValueKind.ShouldBe(JsonValueKind.Array);
        elementos.EnumerateArray().Select(e => (Id(e), Nombre(e))).ShouldBe(
        [
            (1, "Escolarizada"),
            (2, "Abierta"),
            (3, "Virtual"),
            (4, "Mixta"),
            (5, "A distancia"),
            (6, "Semiescolarizada"),
        ]);
    }

    [Fact]
    public async Task Listar_NivelesFormacion_DevuelveLosSeisEnOrdenDeId()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri("niveles-formacion"), Cancelacion);
        var elementos = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        elementos.ValueKind.ShouldBe(JsonValueKind.Array);
        elementos.EnumerateArray().Select(e => (Id(e), Clave(e), Nombre(e))).ShouldBe(
        [
            (1, "TEC", "Técnico"),
            (2, "TSU", "Técnico Superior Universitario"),
            (3, "LIC", "Licenciatura"),
            (4, "ESP", "Especialización"),
            (5, "MAE", "Maestría"),
            (6, "DOC", "Doctorado"),
        ]);
    }

    [Fact]
    public async Task Listar_AreasFormacion_DevuelveLasTresEnOrdenDeId()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri("areas-formacion"), Cancelacion);
        var elementos = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        elementos.ValueKind.ShouldBe(JsonValueKind.Array);
        elementos.EnumerateArray().Select(e => (Id(e), Clave(e), Nombre(e))).ShouldBe(
        [
            (1, "111", "Área de Formación Básica"),
            (2, "112", "Área de Formación Disciplinaria"),
            (3, "113", "Área de Formación Terminal"),
        ]);
    }

    [Fact]
    public async Task Obtener_SistemaEducativoExistente_Responde200()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri("sistemas-educativos", "/1"), Cancelacion);
        var sistema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (Id(sistema), Nombre(sistema)).ShouldBe((1, "Escolarizada"));
    }

    [Fact]
    public async Task Obtener_NivelFormacionExistente_Responde200()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri("niveles-formacion", "/3"), Cancelacion);
        var nivel = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (Id(nivel), Clave(nivel), Nombre(nivel)).ShouldBe((3, "LIC", "Licenciatura"));
    }

    [Fact]
    public async Task Obtener_AreaFormacionExistente_Responde200()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri("areas-formacion", "/2"), Cancelacion);
        var area = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        (Id(area), Clave(area), Nombre(area)).ShouldBe((2, "112", "Área de Formación Disciplinaria"));
    }

    [Theory]
    [MemberData(nameof(Catalogos))]
    public async Task Obtener_Inexistente_Responde404ConCodigo(string recurso, string entidad)
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri(recurso, "/99"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problema.GetProperty("codigo").GetString().ShouldBe($"{entidad}.NoEncontrado");
    }

    [Theory]
    [MemberData(nameof(Catalogos))]
    public async Task Escritura_NoExiste_Responde405(string recurso, string entidad)
    {
        _ = entidad;
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        foreach (var (metodo, sufijo) in new[] { ("POST", ""), ("PUT", "/1"), ("DELETE", "/1") })
        {
            using var solicitud = new HttpRequestMessage(new HttpMethod(metodo), Uri(recurso, sufijo))
            {
                Content = JsonContent.Create(new { nombre = "Valor" }),
            };

            using var respuesta = await cliente.SendAsync(solicitud, Cancelacion);

            respuesta.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed, $"{metodo} {recurso}{sufijo}");
        }
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static int Id(JsonElement elemento) => elemento.GetProperty("id").GetInt32();

    private static string? Clave(JsonElement elemento) => elemento.GetProperty("clave").GetString();

    private static string? Nombre(JsonElement elemento) => elemento.GetProperty("nombre").GetString();

    private static Uri Uri(string recurso, string sufijo = "") =>
        new($"/api/v1/catalogos/{recurso}{sufijo}", UriKind.Relative);

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>(Cancelacion);
}
