using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests.Catalogos;

/// <summary>
/// Catálogos fijos que aún no tienen valores en la semilla: solo se leen, y hoy el listado está vacío.
/// Cuando la semilla los cargue, cada uno pasa a tener su prueba de listado exacto, como grados académicos.
/// </summary>
public sealed class CatalogosFijosEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    public static TheoryData<string, string> Catalogos() => new()
    {
        { "tipos-documento-expediente", "TipoDocumentoExpediente" },
        { "modalidades-recepcion", "ModalidadRecepcion" },
        { "tipos-plaza", "TipoPlaza" },
        { "tipos-contratacion", "TipoContratacion" },
    };

    [Theory]
    [MemberData(nameof(Catalogos))]
    public async Task Listar_SinValoresEnLaSemilla_DevuelveUnArregloVacio(string recurso, string entidad)
    {
        _ = entidad;
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri(recurso), Cancelacion);
        var elementos = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        elementos.ValueKind.ShouldBe(JsonValueKind.Array);
        elementos.GetArrayLength().ShouldBe(0);
    }

    [Theory]
    [MemberData(nameof(Catalogos))]
    public async Task Obtener_Inexistente_Responde404ConCodigo(string recurso, string entidad)
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri(recurso, "/1"), Cancelacion);
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

    private static Uri Uri(string recurso, string sufijo = "") =>
        new($"/api/v1/catalogos/{recurso}{sufijo}", UriKind.Relative);

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>(Cancelacion);
}
