using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests;

public sealed class AutorizacionTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private const string RutaAreas = "/api/v1/institucional/areas-academicas";
    private const string RutaEntidades = "/api/v1/institucional/entidades-academicas";
    private const string RutaArticulos = "/api/v1/catalogos/articulos";

    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("/api/v1/institucional/regiones")]
    [InlineData("/api/v1/catalogos/grados-academicos")]
    public async Task Consultar_SinToken_Responde401ConNoAutenticado(string ruta)
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.GetAsync(new Uri(ruta, UriKind.Relative), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        problema.GetProperty("codigo").GetString().ShouldBe("Autenticacion.NoAutenticado");
    }

    [Theory]
    [InlineData("dgaa")]
    [InlineData("entidad")]
    public async Task Consultar_ConTokenDeUsuarioDeAmbito_Responde200EnRegionesCampusAreasYCatalogos(string rol)
    {
        using var clienteSuperusuario = await _api.CrearClienteSuperusuarioAsync();
        var (areaId, entidadId) = await CrearAreaConEntidadAsync(clienteSuperusuario);
        using var cliente = await CrearClienteDeAmbitoAsync(rol, areaId, entidadId);

        foreach (var ruta in new[]
        {
            "/api/v1/institucional/regiones",
            "/api/v1/institucional/campus",
            RutaAreas,
            "/api/v1/catalogos/grados-academicos",
        })
        {
            using var respuesta = await cliente.GetAsync(new Uri(ruta, UriKind.Relative), Cancelacion);

            respuesta.StatusCode.ShouldBe(HttpStatusCode.OK, ruta);
        }
    }

    [Theory]
    [InlineData("/api/v1/catalogos/sistemas-educativos")]
    [InlineData("/api/v1/catalogos/niveles-formacion")]
    [InlineData("/api/v1/catalogos/areas-formacion")]
    public async Task ConsultarClasificacionesAcademicas_SinToken_Responde401ConNoAutenticado(string ruta)
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.GetAsync(new Uri(ruta, UriKind.Relative), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        problema.GetProperty("codigo").GetString().ShouldBe("Autenticacion.NoAutenticado");
    }

    [Theory]
    [InlineData("dgaa")]
    [InlineData("entidad")]
    public async Task ConsultarClasificacionesAcademicas_ConTokenDeUsuarioDeAmbito_Responde200(string rol)
    {
        using var clienteSuperusuario = await _api.CrearClienteSuperusuarioAsync();
        var (areaId, entidadId) = await CrearAreaConEntidadAsync(clienteSuperusuario);
        using var cliente = await CrearClienteDeAmbitoAsync(rol, areaId, entidadId);

        foreach (var ruta in new[]
        {
            "/api/v1/catalogos/sistemas-educativos",
            "/api/v1/catalogos/niveles-formacion",
            "/api/v1/catalogos/areas-formacion",
        })
        {
            using var respuesta = await cliente.GetAsync(new Uri(ruta, UriKind.Relative), Cancelacion);

            respuesta.StatusCode.ShouldBe(HttpStatusCode.OK, ruta);
        }
    }

    public static TheoryData<string, string> Escrituras()
    {
        var datos = new TheoryData<string, string>();
        foreach (var rol in new[] { "dgaa", "entidad" })
        {
            foreach (var operacion in new[]
            {
                "POST area", "PUT area", "DELETE area",
                "POST entidad", "PUT entidad", "DELETE entidad",
                "POST articulo", "PUT articulo",
            })
            {
                datos.Add(rol, operacion);
            }
        }

        return datos;
    }

    [Theory]
    [MemberData(nameof(Escrituras))]
    public async Task Escribir_ConTokenDeUsuarioDeAmbito_Responde403ConSinPermiso(string rol, string operacion)
    {
        using var clienteSuperusuario = await _api.CrearClienteSuperusuarioAsync();
        var (areaId, entidadId) = await CrearAreaConEntidadAsync(clienteSuperusuario);
        var articuloId = await CrearArticuloAsync(clienteSuperusuario);
        using var cliente = await CrearClienteDeAmbitoAsync(rol, areaId, entidadId);

        var (metodo, ruta, cuerpo) = operacion switch
        {
            "POST area" => (HttpMethod.Post, RutaAreas, CuerpoArea()),
            "PUT area" => (HttpMethod.Put, $"{RutaAreas}/{areaId}", CuerpoArea()),
            "DELETE area" => (HttpMethod.Delete, $"{RutaAreas}/{areaId}", null),
            "POST entidad" => (HttpMethod.Post, RutaEntidades, CuerpoEntidad(areaId)),
            "PUT entidad" => (HttpMethod.Put, $"{RutaEntidades}/{entidadId}", CuerpoEntidad(areaId)),
            "DELETE entidad" => (HttpMethod.Delete, $"{RutaEntidades}/{entidadId}", null),
            "POST articulo" => (HttpMethod.Post, RutaArticulos, CuerpoArticulo()),
            "PUT articulo" => (HttpMethod.Put, $"{RutaArticulos}/{articuloId}", CuerpoArticulo()),
            _ => throw new ArgumentOutOfRangeException(nameof(operacion), operacion, null),
        };
        using var solicitud = new HttpRequestMessage(metodo, new Uri(ruta, UriKind.Relative));
        if (cuerpo is not null)
        {
            solicitud.Content = JsonContent.Create(cuerpo);
        }

        using var respuesta = await cliente.SendAsync(solicitud, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        problema.GetProperty("codigo").GetString().ShouldBe("Autorizacion.SinPermiso");
    }

    [Fact]
    public async Task Dgaa_ListaYObtieneSoloLasEntidadesDeSuArea()
    {
        using var clienteSuperusuario = await _api.CrearClienteSuperusuarioAsync();
        var (areaA, entidadA) = await CrearAreaConEntidadAsync(clienteSuperusuario);
        var (areaB, entidadB) = await CrearAreaConEntidadAsync(clienteSuperusuario);
        using var cliente = await _api.CrearClienteDgaaAsync(areaA);

        using var listadoA = await cliente.GetAsync(
            new Uri($"{RutaEntidades}?areaAcademicaId={areaA}", UriKind.Relative), Cancelacion);
        var paginaA = await Leer(listadoA);
        using var listadoB = await cliente.GetAsync(
            new Uri($"{RutaEntidades}?areaAcademicaId={areaB}", UriKind.Relative), Cancelacion);
        var paginaB = await Leer(listadoB);
        using var deB = await cliente.GetAsync(new Uri($"{RutaEntidades}/{entidadB}", UriKind.Relative), Cancelacion);
        var problema = await Leer(deB);
        using var deA = await cliente.GetAsync(new Uri($"{RutaEntidades}/{entidadA}", UriKind.Relative), Cancelacion);

        listadoA.StatusCode.ShouldBe(HttpStatusCode.OK);
        Ids(paginaA).ShouldContain(entidadA);
        listadoB.StatusCode.ShouldBe(HttpStatusCode.OK);
        Ids(paginaB).ShouldNotContain(entidadB);
        paginaB.GetProperty("total").GetInt32().ShouldBe(0);
        deB.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problema.GetProperty("codigo").GetString().ShouldBe("EntidadAcademica.NoEncontrado");
        deA.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EntidadAcademica_ListaYObtieneSoloSuEntidad()
    {
        using var clienteSuperusuario = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearAreaAsync(clienteSuperusuario);
        var entidadX = await CrearEntidadAsync(clienteSuperusuario, areaId);
        var entidadY = await CrearEntidadAsync(clienteSuperusuario, areaId);
        using var cliente = await _api.CrearClienteEntidadAcademicaAsync(entidadX);

        using var listado = await cliente.GetAsync(
            new Uri($"{RutaEntidades}?areaAcademicaId={areaId}", UriKind.Relative), Cancelacion);
        var pagina = await Leer(listado);
        using var deY = await cliente.GetAsync(new Uri($"{RutaEntidades}/{entidadY}", UriKind.Relative), Cancelacion);
        var problema = await Leer(deY);
        using var deX = await cliente.GetAsync(new Uri($"{RutaEntidades}/{entidadX}", UriKind.Relative), Cancelacion);

        listado.StatusCode.ShouldBe(HttpStatusCode.OK);
        Ids(pagina).ShouldBe([entidadX]);
        pagina.GetProperty("total").GetInt32().ShouldBe(1);
        deY.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problema.GetProperty("codigo").GetString().ShouldBe("EntidadAcademica.NoEncontrado");
        deX.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Consultar_ConContrasenaPendiente_Responde403ConCambioContrasenaPendiente()
    {
        var correo = await _api.CrearSuperusuarioAsync();
        using var cliente = _api.CreateClient();
        using var login = await cliente.PostAsJsonAsync(
            new Uri("/api/v1/usuarios/iniciar-sesion", UriKind.Relative),
            new { correo, contrasena = SgplaApiFactory.ContrasenaConocidaSuperusuario },
            Cancelacion);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        var sesion = await Leer(login);
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", sesion.GetProperty("token").GetString());

        using var respuesta = await cliente.GetAsync(
            new Uri("/api/v1/catalogos/grados-academicos", UriKind.Relative), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        problema.GetProperty("codigo").GetString().ShouldBe("Autenticacion.CambioContrasenaPendiente");
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>(Cancelacion);

    private static List<int> Ids(JsonElement pagina) =>
        pagina.GetProperty("elementos").EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToList();

    private Task<HttpClient> CrearClienteDeAmbitoAsync(string rol, int areaId, int entidadId) =>
        rol == "dgaa" ? _api.CrearClienteDgaaAsync(areaId) : _api.CrearClienteEntidadAcademicaAsync(entidadId);

    private static async Task<(int AreaId, int EntidadId)> CrearAreaConEntidadAsync(HttpClient cliente)
    {
        var areaId = await CrearAreaAsync(cliente);
        return (areaId, await CrearEntidadAsync(cliente, areaId));
    }

    private static async Task<int> CrearAreaAsync(HttpClient cliente)
    {
        using var respuesta = await cliente.PostAsJsonAsync(
            new Uri(RutaAreas, UriKind.Relative), CuerpoArea(), Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await Leer(respuesta)).GetProperty("id").GetInt32();
    }

    private static async Task<int> CrearEntidadAsync(HttpClient cliente, int areaAcademicaId)
    {
        using var respuesta = await cliente.PostAsJsonAsync(
            new Uri(RutaEntidades, UriKind.Relative), CuerpoEntidad(areaAcademicaId), Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await Leer(respuesta)).GetProperty("id").GetInt32();
    }

    private static async Task<int> CrearArticuloAsync(HttpClient cliente)
    {
        using var respuesta = await cliente.PostAsJsonAsync(
            new Uri(RutaArticulos, UriKind.Relative), CuerpoArticulo(), Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await Leer(respuesta)).GetProperty("id").GetInt32();
    }

    private static object CuerpoArea() => new
    {
        clave = DatosUnicos.ClaveEntera(),
        nombre = "Facultad de Prueba",
        telefono = "2288421700",
        extension = (string?)null,
    };

    private static object CuerpoArticulo() => new { numero = DatosUnicos.Numero(), descripcion = "Fundamento de prueba." };

    private static object CuerpoEntidad(int areaAcademicaId) => new
    {
        clave = DatosUnicos.ClaveAlfanumerica(),
        nombre = "Entidad de Prueba",
        calle = "Calle de prueba",
        numeroExterior = (string?)null,
        colonia = "Colonia de prueba",
        codigoPostal = "91020",
        telefono = "2288421700",
        extension = (string?)null,
        campusId = 1,
        areaAcademicaId,
        municipioId = 87,
    };
}
