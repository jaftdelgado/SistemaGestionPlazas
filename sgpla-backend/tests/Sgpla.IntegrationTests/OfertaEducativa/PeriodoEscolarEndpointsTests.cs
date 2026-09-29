using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests.OfertaEducativa;

public sealed class PeriodoEscolarEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private const string Ruta = "/api/v1/oferta-educativa/periodos-escolares";

    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Crear_ConDatosValidos_Responde201ConLocationYElRecurso()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var clave = DatosUnicos.ClavePeriodo();
        var cuerpo = CuerpoValido($" {clave} ");

        using var respuesta = await cliente.PostAsJsonAsync(Uri(), cuerpo, Cancelacion);
        var creado = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
        creado.GetProperty("clave").GetString().ShouldBe(clave);
        creado.GetProperty("fechaInicio").GetString().ShouldBe("2026-08-10");
        creado.GetProperty("fechaFin").GetString().ShouldBe("2027-01-22");
        respuesta.Headers.Location.ShouldNotBeNull()
            .AbsolutePath.ShouldBe($"{Ruta}/{creado.GetProperty("id").GetInt32()}");
    }

    [Theory]
    [InlineData("clave", "")]
    [InlineData("clave", "   ")]
    [InlineData("clave", "20270")]
    [InlineData("clave", "2027A1")]
    [InlineData("fechaFin", "2026-08-09")]
    public async Task Crear_ConCampoInvalido_Responde400ConErrorEnElCampo(string campo, string valor)
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var cuerpo = CuerpoValido();
        cuerpo[campo] = valor;

        using var respuesta = await cliente.PostAsJsonAsync(Uri(), cuerpo, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty(campo, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Crear_ConClaveDuplicada_Responde409()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var (_, clave) = await CrearAsync(cliente);

        using var respuesta = await cliente.PostAsJsonAsync(Uri(), CuerpoValido(clave), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("PeriodoEscolar.ClaveDuplicada");
    }

    [Fact]
    public async Task Crear_ConClaveDeUnPeriodoDadoDeBaja_Responde409()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var (id, clave) = await CrearAsync(cliente);
        (await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await cliente.PostAsJsonAsync(Uri(), CuerpoValido(clave), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("PeriodoEscolar.ClaveDuplicada");
    }

    [Fact]
    public async Task Listar_DevuelveLosPeriodosEnOrdenDeClaveDescendente()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var claves = new[] { DatosUnicos.ClavePeriodo(), DatosUnicos.ClavePeriodo(), DatosUnicos.ClavePeriodo() };
        var idPorClave = new Dictionary<string, int>();
        foreach (var clave in new[] { claves[1], claves[2], claves[0] })
        {
            idPorClave[clave] = (await CrearAsync(cliente, clave)).Id;
        }

        using var respuesta = await cliente.GetAsync(Uri(), Cancelacion);
        var periodos = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var propios = idPorClave.Values.ToHashSet();
        periodos.EnumerateArray()
            .Select(p => p.GetProperty("id").GetInt32())
            .Where(propios.Contains)
            .ShouldBe([idPorClave[claves[2]], idPorClave[claves[1]], idPorClave[claves[0]]]);
    }

    [Fact]
    public async Task Listar_ExcluyeLosPeriodosDadosDeBaja()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var (idActivo, _) = await CrearAsync(cliente);
        var (idBaja, _) = await CrearAsync(cliente);
        (await cliente.DeleteAsync(Uri($"/{idBaja}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await cliente.GetAsync(Uri(), Cancelacion);
        var ids = (await Leer(respuesta)).EnumerateArray().Select(p => p.GetProperty("id").GetInt32()).ToList();

        ids.ShouldContain(idActivo);
        ids.ShouldNotContain(idBaja);
    }

    [Fact]
    public async Task Obtener_Existente_Responde200()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var (id, clave) = await CrearAsync(cliente);

        using var respuesta = await cliente.GetAsync(Uri($"/{id}"), Cancelacion);
        var periodo = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        periodo.GetProperty("clave").GetString().ShouldBe(clave);
    }

    [Fact]
    public async Task Obtener_Inexistente_Responde404ConCodigo()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri($"/{int.MaxValue}"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problema.GetProperty("codigo").GetString().ShouldBe("PeriodoEscolar.NoEncontrado");
    }

    [Fact]
    public async Task Obtener_DadoDeBaja_Responde404()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var (id, _) = await CrearAsync(cliente);
        (await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await cliente.GetAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Modificar_ConFechasValidas_Responde204YCambiaSoloLasFechas()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var (id, clave) = await CrearAsync(cliente);

        using var respuesta = await cliente.PutAsJsonAsync(
            Uri($"/{id}"), new { fechaInicio = "2026-08-17", fechaFin = "2027-02-05" }, Cancelacion);
        using var consulta = await cliente.GetAsync(Uri($"/{id}"), Cancelacion);
        var periodo = await Leer(consulta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        periodo.GetProperty("clave").GetString().ShouldBe(clave);
        periodo.GetProperty("fechaInicio").GetString().ShouldBe("2026-08-17");
        periodo.GetProperty("fechaFin").GetString().ShouldBe("2027-02-05");
    }

    [Fact]
    public async Task Modificar_ConProgramaciones_Responde204()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var (periodoId, _) = await CrearAsync(cliente);
        var entidadId = await CrearEntidadAsync(cliente, await CrearAreaAsync(cliente));
        await DatosAcademicosSql.InsertarProgramacionAsync(sqlServer.CadenaConexion, periodoId, entidadId);

        using var respuesta = await cliente.PutAsJsonAsync(
            Uri($"/{periodoId}"), new { fechaInicio = "2026-08-17", fechaFin = "2027-02-05" }, Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Modificar_ConRangoInvalido_Responde400ConErrorEnFechaFin()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var (id, _) = await CrearAsync(cliente);

        using var respuesta = await cliente.PutAsJsonAsync(
            Uri($"/{id}"), new { fechaInicio = "2027-02-05", fechaFin = "2026-08-17" }, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty("fechaFin", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Modificar_Inexistente_Responde404()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.PutAsJsonAsync(
            Uri($"/{int.MaxValue}"), new { fechaInicio = "2026-08-17", fechaFin = "2027-02-05" }, Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DarDeBaja_SinReferencias_Responde204()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var (id, _) = await CrearAsync(cliente);

        using var respuesta = await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DarDeBaja_YaDadoDeBaja_Responde404()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var (id, _) = await CrearAsync(cliente);
        (await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DarDeBaja_Inexistente_Responde404()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.DeleteAsync(Uri($"/{int.MaxValue}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DarDeBaja_ConUnaProgramacion_Responde409(bool programacionDadaDeBaja)
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var (periodoId, _) = await CrearAsync(cliente);
        var entidadId = await CrearEntidadAsync(cliente, await CrearAreaAsync(cliente));
        await DatosAcademicosSql.InsertarProgramacionAsync(
            sqlServer.CadenaConexion, periodoId, entidadId, programacionDadaDeBaja);

        using var respuesta = await cliente.DeleteAsync(Uri($"/{periodoId}"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("PeriodoEscolar.TieneReferencias");
    }

    [Fact]
    public async Task Escribir_ComoDgaa_Responde403()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        var (id, _) = await CrearAsync(superusuario);
        using var dgaa = await _api.CrearClienteDgaaAsync(await CrearAreaAsync(superusuario));

        using var crear = await dgaa.PostAsJsonAsync(Uri(), CuerpoValido(), Cancelacion);
        using var modificar = await dgaa.PutAsJsonAsync(
            Uri($"/{id}"), new { fechaInicio = "2026-08-17", fechaFin = "2027-02-05" }, Cancelacion);
        using var darDeBaja = await dgaa.DeleteAsync(Uri($"/{id}"), Cancelacion);

        crear.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        modificar.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        darDeBaja.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Leer_ComoDgaa_Responde200()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        var (id, _) = await CrearAsync(superusuario);
        using var dgaa = await _api.CrearClienteDgaaAsync(await CrearAreaAsync(superusuario));

        using var listado = await dgaa.GetAsync(Uri(), Cancelacion);
        using var obtenido = await dgaa.GetAsync(Uri($"/{id}"), Cancelacion);

        listado.StatusCode.ShouldBe(HttpStatusCode.OK);
        obtenido.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static Uri Uri(string sufijo = "") => new($"{Ruta}{sufijo}", UriKind.Relative);

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>(Cancelacion);

    private static JsonObject CuerpoValido(string? clave = null) => new()
    {
        ["clave"] = clave ?? DatosUnicos.ClavePeriodo(),
        ["fechaInicio"] = "2026-08-10",
        ["fechaFin"] = "2027-01-22",
    };

    private static async Task<(int Id, string Clave)> CrearAsync(HttpClient cliente, string? clave = null)
    {
        var cuerpo = CuerpoValido(clave);
        using var respuesta = await cliente.PostAsJsonAsync(Uri(), cuerpo, Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var creado = await Leer(respuesta);

        return (creado.GetProperty("id").GetInt32(), creado.GetProperty("clave").GetString()!);
    }

    private static async Task<int> CrearAreaAsync(HttpClient cliente)
    {
        using var respuesta = await cliente.PostAsJsonAsync(
            new Uri("/api/v1/institucional/areas-academicas", UriKind.Relative),
            new { clave = DatosUnicos.ClaveEntera(), nombre = "Área de prueba", telefono = "2288421700", extension = (string?)null },
            Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await Leer(respuesta)).GetProperty("id").GetInt32();
    }

    private static async Task<int> CrearEntidadAsync(HttpClient cliente, int areaAcademicaId)
    {
        using var respuesta = await cliente.PostAsJsonAsync(
            new Uri("/api/v1/institucional/entidades-academicas", UriKind.Relative),
            new
            {
                clave = DatosUnicos.ClaveAlfanumerica(),
                nombre = "Facultad de prueba",
                calle = "Calle de prueba",
                numeroExterior = (string?)null,
                colonia = "Colonia de prueba",
                codigoPostal = "91020",
                telefono = "2288421700",
                extension = (string?)null,
                campusId = 1,
                areaAcademicaId,
                municipioId = 87,
            },
            Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await Leer(respuesta)).GetProperty("id").GetInt32();
    }
}
