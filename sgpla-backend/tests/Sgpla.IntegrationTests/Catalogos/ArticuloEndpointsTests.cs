using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests.Catalogos;

public sealed class ArticuloEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private const string Ruta = "/api/v1/catalogos/articulos";
    private const string Descripcion = "Fundamento de prueba.";

    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Crear_ConNumeroNuevo_Responde201ConUbicacionYValoresNormalizados()
    {
        using var cliente = _api.CreateClient();
        var numero = DatosUnicos.Numero();

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(), new { numero = $"  {numero.ToLowerInvariant()}   bis ", descripcion = $"  {Descripcion} " }, Cancelacion);
        var creado = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
        creado.GetProperty("numero").GetString().ShouldBe($"{numero} BIS");
        creado.GetProperty("descripcion").GetString().ShouldBe(Descripcion);
        respuesta.Headers.Location.ShouldNotBeNull().AbsolutePath.ShouldBe($"{Ruta}/{creado.GetProperty("id").GetInt32()}");
    }

    [Theory]
    [InlineData("", Descripcion, "numero")]
    [InlineData("42", "", "descripcion")]
    [InlineData("42", "   ", "descripcion")]
    [InlineData("42", null, "descripcion")]
    public async Task Crear_SinNumeroODescripcion_Responde400ConErrorEnElCampo(string numero, string? descripcion, string campo)
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.PostAsJsonAsync(Uri(), new { numero, descripcion }, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty(campo, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Crear_ConNumeroNoAscii_Responde400ConCodigo()
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(), new { numero = $"{DatosUnicos.Numero()} BÍS", descripcion = Descripcion }, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("codigo").GetString().ShouldBe("Articulo.NumeroNoAscii");
    }

    [Fact]
    public async Task Crear_ConNumeroEquivalenteTrasNormalizar_Responde409()
    {
        using var cliente = _api.CreateClient();
        var numero = DatosUnicos.Numero();
        await Crear(cliente, $"{numero} BIS");

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(), new { numero = $" {numero.ToLowerInvariant()}  bis", descripcion = Descripcion }, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("Articulo.NumeroDuplicado");
    }

    [Fact]
    public async Task Obtener_Existente_Responde200()
    {
        using var cliente = _api.CreateClient();
        var (id, numero) = await Crear(cliente);

        using var respuesta = await cliente.GetAsync(Uri($"/{id}"), Cancelacion);
        var articulo = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        articulo.GetProperty("numero").GetString().ShouldBe(numero);
        articulo.GetProperty("descripcion").GetString().ShouldBe(Descripcion);
    }

    [Fact]
    public async Task Obtener_Inexistente_Responde404ConCodigo()
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.GetAsync(Uri($"/{int.MaxValue}"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problema.GetProperty("codigo").GetString().ShouldBe("Articulo.NoEncontrado");
    }

    [Fact]
    public async Task Listar_DevuelveUnArregloOrdenadoPorNumeroComoTexto()
    {
        using var cliente = _api.CreateClient();
        var prefijo = DatosUnicos.Numero();
        var (id10, _) = await Crear(cliente, $"{prefijo} 10");
        var (id9, _) = await Crear(cliente, $"{prefijo} 9");
        var (id10Bis, _) = await Crear(cliente, $"{prefijo} 10 BIS");

        using var respuesta = await cliente.GetAsync(Uri(), Cancelacion);
        var articulos = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        articulos.ValueKind.ShouldBe(JsonValueKind.Array);
        articulos.EnumerateArray()
            .Select(a => a.GetProperty("id").GetInt32())
            .Where(id => id == id10 || id == id9 || id == id10Bis)
            .ShouldBe([id10, id10Bis, id9]);
    }

    [Fact]
    public async Task Modificar_NumeroYDescripcion_Responde204()
    {
        using var cliente = _api.CreateClient();
        var (id, _) = await Crear(cliente);
        var nuevoNumero = DatosUnicos.Numero();

        using var respuesta = await cliente.PutAsJsonAsync(
            Uri($"/{id}"), new { numero = nuevoNumero, descripcion = "Nueva descripción." }, Cancelacion);
        using var consulta = await cliente.GetAsync(Uri($"/{id}"), Cancelacion);
        var articulo = await Leer(consulta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        articulo.GetProperty("numero").GetString().ShouldBe(nuevoNumero);
        articulo.GetProperty("descripcion").GetString().ShouldBe("Nueva descripción.");
    }

    [Fact]
    public async Task Modificar_SinDescripcion_ConservaLaActual()
    {
        using var cliente = _api.CreateClient();
        var (id, _) = await Crear(cliente);
        var nuevoNumero = DatosUnicos.Numero();

        using var respuesta = await cliente.PutAsJsonAsync(Uri($"/{id}"), new { numero = nuevoNumero }, Cancelacion);
        using var consulta = await cliente.GetAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Leer(consulta)).GetProperty("descripcion").GetString().ShouldBe(Descripcion);
    }

    [Fact]
    public async Task Modificar_ConDescripcionVacia_Responde400()
    {
        using var cliente = _api.CreateClient();
        var (id, numero) = await Crear(cliente);

        using var respuesta = await cliente.PutAsJsonAsync(Uri($"/{id}"), new { numero, descripcion = "  " }, Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Modificar_ConNumeroDeOtroArticulo_Responde409()
    {
        using var cliente = _api.CreateClient();
        var (_, existente) = await Crear(cliente);
        var (id, _) = await Crear(cliente);

        using var respuesta = await cliente.PutAsJsonAsync(
            Uri($"/{id}"), new { numero = existente.ToLowerInvariant() }, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("Articulo.NumeroDuplicado");
    }

    [Fact]
    public async Task Modificar_Inexistente_Responde404()
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.PutAsJsonAsync(
            Uri($"/{int.MaxValue}"), new { numero = "42", descripcion = Descripcion }, Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Eliminar_NoExiste_Responde405()
    {
        using var cliente = _api.CreateClient();
        var (id, _) = await Crear(cliente);

        using var respuesta = await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static Uri Uri(string sufijo = "") => new($"{Ruta}{sufijo}", UriKind.Relative);

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>(Cancelacion);

    private static async Task<(int Id, string Numero)> Crear(HttpClient cliente, string? numero = null)
    {
        numero ??= DatosUnicos.Numero();
        using var respuesta = await cliente.PostAsJsonAsync(Uri(), new { numero, descripcion = Descripcion }, Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return ((await Leer(respuesta)).GetProperty("id").GetInt32(), numero);
    }
}
