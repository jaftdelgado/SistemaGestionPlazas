using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests.Catalogos;

/// <summary>Catálogo fijo: solo se lee lo que carga la semilla (DATABASE.md §6.21).</summary>
public sealed class GradoAcademicoEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private const string Ruta = "/api/v1/catalogos/grados-academicos";

    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Listar_DevuelveLosCuatroGradosEnOrdenDeId()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri(), Cancelacion);
        var grados = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        grados.ValueKind.ShouldBe(JsonValueKind.Array);
        grados.EnumerateArray()
            .Select(g => (g.GetProperty("id").GetInt32(), g.GetProperty("nombre").GetString()))
            .ShouldBe([(1, "Licenciatura"), (2, "Especialidad"), (3, "Maestría"), (4, "Doctorado")]);
    }

    [Fact]
    public async Task Obtener_Existente_Responde200()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri("/3"), Cancelacion);
        var grado = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        grado.GetProperty("id").GetInt32().ShouldBe(3);
        grado.GetProperty("nombre").GetString().ShouldBe("Maestría");
    }

    [Fact]
    public async Task Obtener_Inexistente_Responde404ConCodigo()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri("/99"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problema.GetProperty("codigo").GetString().ShouldBe("GradoAcademico.NoEncontrado");
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
            Content = JsonContent.Create(new { nombre = "Posdoctorado" }),
        };

        using var respuesta = await cliente.SendAsync(solicitud, Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static Uri Uri(string sufijo = "") => new($"{Ruta}{sufijo}", UriKind.Relative);

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>(Cancelacion);
}
