using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests.Institucional;

public sealed class AreaAcademicaEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private const string Ruta = "/api/v1/institucional/areas-academicas";

    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Crear_ConDatosValidos_Responde201ConUbicacionYValoresNormalizados()
    {
        using var cliente = _api.CreateClient();
        var clave = DatosUnicos.ClaveEntera();

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(),
            new { clave, nombre = "  Facultad   de  Letras ", telefono = "2288421700", extension = "11350" },
            Cancelacion);
        var creada = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
        creada.GetProperty("clave").GetInt32().ShouldBe(clave);
        creada.GetProperty("nombre").GetString().ShouldBe("Facultad de Letras");
        creada.GetProperty("telefono").GetString().ShouldBe("2288421700");
        creada.GetProperty("extension").GetString().ShouldBe("11350");
        respuesta.Headers.Location.ShouldNotBeNull().AbsolutePath.ShouldBe($"{Ruta}/{creada.GetProperty("id").GetInt32()}");
    }

    [Theory]
    [InlineData(0, "Facultad de Letras", "2288421700", null, "clave")]
    [InlineData(1, "", "2288421700", null, "nombre")]
    [InlineData(1, "Facultad de Letras", "228842170", null, "telefono")]
    [InlineData(1, "Facultad de Letras", "228 842 1700", null, "telefono")]
    [InlineData(1, "Facultad de Letras", "2288421700", "12A", "extension")]
    public async Task Crear_ConCampoInvalido_Responde400ConErrorEnElCampo(
        int clave, string nombre, string telefono, string? extension, string campo)
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.PostAsJsonAsync(Uri(), new { clave, nombre, telefono, extension }, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty(campo, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Crear_ConClaveExistente_Responde409()
    {
        using var cliente = _api.CreateClient();
        var (_, clave) = await Crear(cliente);

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(), new { clave, nombre = "Otra facultad", telefono = "2288421700", extension = (string?)null }, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("AreaAcademica.ClaveDuplicada");
    }

    [Fact]
    public async Task Crear_ConClaveDeUnAreaDadaDeBaja_Responde409()
    {
        using var cliente = _api.CreateClient();
        var (id, clave) = await Crear(cliente);
        (await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(), new { clave, nombre = "Otra facultad", telefono = "2288421700", extension = (string?)null }, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("AreaAcademica.ClaveDuplicada");
    }

    [Fact]
    public async Task Obtener_Existente_Responde200()
    {
        using var cliente = _api.CreateClient();
        var (id, clave) = await Crear(cliente);

        using var respuesta = await cliente.GetAsync(Uri($"/{id}"), Cancelacion);
        var area = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        area.GetProperty("clave").GetInt32().ShouldBe(clave);
    }

    [Fact]
    public async Task Obtener_Inexistente_Responde404ConCodigo()
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.GetAsync(Uri($"/{int.MaxValue}"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problema.GetProperty("codigo").GetString().ShouldBe("AreaAcademica.NoEncontrado");
    }

    [Fact]
    public async Task Obtener_DadaDeBaja_Responde404()
    {
        using var cliente = _api.CreateClient();
        var (id, _) = await Crear(cliente);
        (await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await cliente.GetAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Listar_IncluyeLasCreadasEnOrdenDeClaveYExcluyeLasDadasDeBaja()
    {
        using var cliente = _api.CreateClient();
        var baseClave = DatosUnicos.ClaveEntera() % 1_000_000 + 1;
        var (idMenor, claveMenor) = await Crear(cliente, baseClave);
        var (idMedia, claveMedia) = await Crear(cliente, baseClave + 1);
        var (idBaja, claveBaja) = await Crear(cliente, baseClave + 2);
        (await cliente.DeleteAsync(Uri($"/{idBaja}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await cliente.GetAsync(Uri(), Cancelacion);
        var areas = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var claves = areas.EnumerateArray()
            .Select(a => a.GetProperty("clave").GetInt32())
            .Where(clave => clave == claveMenor || clave == claveMedia || clave == claveBaja)
            .ToList();
        claves.ShouldBe([claveMenor, claveMedia]);
        idMenor.ShouldNotBe(idBaja);
    }

    [Fact]
    public async Task Modificar_Responde204YPersisteLosCambios()
    {
        using var cliente = _api.CreateClient();
        var (id, _) = await Crear(cliente);

        using var respuesta = await cliente.PutAsJsonAsync(
            Uri($"/{id}"), new { nombre = "Facultad de Física", telefono = "2288421701", extension = "11351" }, Cancelacion);
        using var consulta = await cliente.GetAsync(Uri($"/{id}"), Cancelacion);
        var area = await Leer(consulta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        area.GetProperty("nombre").GetString().ShouldBe("Facultad de Física");
        area.GetProperty("telefono").GetString().ShouldBe("2288421701");
        area.GetProperty("extension").GetString().ShouldBe("11351");
    }

    [Fact]
    public async Task Modificar_SinExtension_LaQuita()
    {
        using var cliente = _api.CreateClient();
        var (id, _) = await Crear(cliente, extension: "11350");

        using var respuesta = await cliente.PutAsJsonAsync(
            Uri($"/{id}"), new { nombre = "Facultad de Letras", telefono = "2288421700", extension = (string?)null }, Cancelacion);
        using var consulta = await cliente.GetAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Leer(consulta)).GetProperty("extension").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Modificar_Inexistente_Responde404()
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.PutAsJsonAsync(
            Uri($"/{int.MaxValue}"), new { nombre = "Facultad de Letras", telefono = "2288421700", extension = (string?)null }, Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Modificar_DadaDeBaja_Responde404()
    {
        using var cliente = _api.CreateClient();
        var (id, _) = await Crear(cliente);
        (await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await cliente.PutAsJsonAsync(
            Uri($"/{id}"), new { nombre = "Facultad de Letras", telefono = "2288421700", extension = (string?)null }, Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DarDeBaja_Activa_Responde204()
    {
        using var cliente = _api.CreateClient();
        var (id, _) = await Crear(cliente);

        using var respuesta = await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DarDeBaja_YaDadaDeBaja_Responde404()
    {
        using var cliente = _api.CreateClient();
        var (id, _) = await Crear(cliente);
        (await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static Uri Uri(string sufijo = "") => new($"{Ruta}{sufijo}", UriKind.Relative);

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>(Cancelacion);

    private static async Task<(int Id, int Clave)> Crear(
        HttpClient cliente, int? clave = null, string? extension = null)
    {
        clave ??= DatosUnicos.ClaveEntera();
        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(), new { clave, nombre = "Facultad de Letras", telefono = "2288421700", extension }, Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return ((await Leer(respuesta)).GetProperty("id").GetInt32(), clave.Value);
    }
}
