using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests.Catalogos;

/// <summary>Catálogo fijo: solo se lee lo que carga la semilla (DATABASE.md §15.2), cada tratamiento con su grado.</summary>
public sealed class TratamientoAcademicoEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private const string Ruta = "/api/v1/catalogos/tratamientos-academicos";

    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Listar_DevuelveLosCincoTratamientosConSuGradoEnOrdenDeId()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri(), Cancelacion);
        var tratamientos = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        tratamientos.ValueKind.ShouldBe(JsonValueKind.Array);
        tratamientos.EnumerateArray().Select(Resumir).ShouldBe(
        [
            (1, "Lic", 1, "Licenciatura"),
            (2, "Mtro", 3, "Maestría"),
            (3, "Mtra", 3, "Maestría"),
            (4, "Dr", 4, "Doctorado"),
            (5, "Dra", 4, "Doctorado"),
        ]);
    }

    [Theory]
    [InlineData(3, new[] { 2, 3 })]
    [InlineData(4, new[] { 4, 5 })]
    [InlineData(2, new int[0])]
    [InlineData(99, new int[0])]
    public async Task Listar_FiltradoPorGrado_DevuelveSoloLosDeEseGrado(int gradoAcademicoId, int[] esperados)
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri($"?gradoAcademicoId={gradoAcademicoId}"), Cancelacion);
        var tratamientos = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        tratamientos.EnumerateArray().Select(t => t.GetProperty("id").GetInt32()).ShouldBe(esperados);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Listar_ConGradoNoPositivo_Responde400ConElParametro(int gradoAcademicoId)
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri($"?gradoAcademicoId={gradoAcademicoId}"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty("gradoAcademicoId", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Obtener_Existente_Responde200ConSuGrado()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri("/3"), Cancelacion);
        var tratamiento = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        Resumir(tratamiento).ShouldBe((3, "Mtra", 3, "Maestría"));
    }

    [Fact]
    public async Task Obtener_Inexistente_Responde404ConCodigo()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri("/99"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problema.GetProperty("codigo").GetString().ShouldBe("TratamientoAcademico.NoEncontrado");
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
            Content = JsonContent.Create(new { nombre = "Ing", gradoAcademicoId = 1 }),
        };

        using var respuesta = await cliente.SendAsync(solicitud, Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static (int Id, string? Nombre, int GradoId, string? GradoNombre) Resumir(JsonElement tratamiento)
    {
        var grado = tratamiento.GetProperty("gradoAcademico");
        return (
            tratamiento.GetProperty("id").GetInt32(),
            tratamiento.GetProperty("nombre").GetString(),
            grado.GetProperty("id").GetInt32(),
            grado.GetProperty("nombre").GetString());
    }

    private static Uri Uri(string sufijo = "") => new($"{Ruta}{sufijo}", UriKind.Relative);

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>(Cancelacion);
}
