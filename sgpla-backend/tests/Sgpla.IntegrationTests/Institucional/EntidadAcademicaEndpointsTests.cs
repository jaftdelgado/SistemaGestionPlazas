using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests.Institucional;

public sealed class EntidadAcademicaEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private const string Ruta = "/api/v1/institucional/entidades-academicas";
    private const string RutaAreas = "/api/v1/institucional/areas-academicas";
    private const string RutaProgramas = "/api/v1/oferta-educativa/programas-educativos";

    // Xalapa: campus id 1 (clave "X") y municipio id 87, cargados por la semilla.
    private const int CampusId = 1;
    private const int MunicipioId = 87;

    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Crear_ConDatosValidos_Responde201ConRespuestaAnidadaYValoresNormalizados()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var clave = DatosUnicos.ClaveAlfanumerica();
        var cuerpo = CuerpoValido(areaId, clave.ToLowerInvariant());
        cuerpo["nombre"] = "  Facultad   de  Letras Españolas ";

        using var respuesta = await cliente.PostAsJsonAsync(Uri(), cuerpo, Cancelacion);
        var creada = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
        creada.GetProperty("clave").GetString().ShouldBe(clave);
        creada.GetProperty("nombre").GetString().ShouldBe("Facultad de Letras Españolas");
        creada.GetProperty("campus").GetProperty("clave").GetString().ShouldBe("X");
        creada.GetProperty("campus").GetProperty("region").GetProperty("nombre").GetString().ShouldBe("Xalapa");
        creada.GetProperty("areaAcademica").GetProperty("id").GetInt32().ShouldBe(areaId);
        creada.GetProperty("municipio").GetProperty("nombre").GetString().ShouldBe("Xalapa");
        respuesta.Headers.Location.ShouldNotBeNull()
            .AbsolutePath.ShouldBe($"{Ruta}/{creada.GetProperty("id").GetInt32()}");
    }

    [Theory]
    [InlineData("clave", "")]
    [InlineData("nombre", "")]
    [InlineData("calle", "")]
    [InlineData("numeroExterior", "   ")]
    [InlineData("colonia", "")]
    [InlineData("codigoPostal", "9102")]
    [InlineData("telefono", "228842170")]
    [InlineData("extension", "12A")]
    public async Task Crear_ConCampoInvalido_Responde400ConErrorEnElCampo(string campo, string valor)
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var cuerpo = CuerpoValido(areaId);
        cuerpo[campo] = valor;

        using var respuesta = await cliente.PostAsJsonAsync(Uri(), cuerpo, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty(campo, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Crear_ConCampusInexistente_Responde400ConErrorEnCampusId()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var cuerpo = CuerpoValido(areaId);
        cuerpo["campusId"] = 999;

        using var respuesta = await cliente.PostAsJsonAsync(Uri(), cuerpo, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty("campusId", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Crear_ConAreaAcademicaInexistente_Responde400ConErrorEnAreaAcademicaId()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var cuerpo = CuerpoValido(999);

        using var respuesta = await cliente.PostAsJsonAsync(Uri(), cuerpo, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty("areaAcademicaId", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Crear_ConAreaAcademicaDadaDeBaja_Responde400ConErrorEnAreaAcademicaId()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        (await cliente.DeleteAsync(new Uri($"{RutaAreas}/{areaId}", UriKind.Relative), Cancelacion))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var cuerpo = CuerpoValido(areaId);

        using var respuesta = await cliente.PostAsJsonAsync(Uri(), cuerpo, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty("areaAcademicaId", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Crear_ConMunicipioInexistente_Responde400ConErrorEnMunicipioId()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var cuerpo = CuerpoValido(areaId);
        cuerpo["municipioId"] = 999;

        using var respuesta = await cliente.PostAsJsonAsync(Uri(), cuerpo, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty("municipioId", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Crear_ConClaveExistenteEnMinusculas_Responde409()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var clave = await Crear(cliente, areaId);

        var cuerpo = CuerpoValido(areaId, clave.ToLowerInvariant());
        using var respuesta = await cliente.PostAsJsonAsync(Uri(), cuerpo, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("EntidadAcademica.ClaveDuplicada");
    }

    [Fact]
    public async Task Crear_ConClaveDeUnaEntidadDadaDeBaja_Responde409()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var (id, clave) = await CrearYObtener(cliente, areaId);
        (await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var cuerpo = CuerpoValido(areaId, clave);
        using var respuesta = await cliente.PostAsJsonAsync(Uri(), cuerpo, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("EntidadAcademica.ClaveDuplicada");
    }

    [Fact]
    public async Task Obtener_Existente_Responde200()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var (id, clave) = await CrearYObtener(cliente, areaId);

        using var respuesta = await cliente.GetAsync(Uri($"/{id}"), Cancelacion);
        var entidad = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        entidad.GetProperty("clave").GetString().ShouldBe(clave);
    }

    [Fact]
    public async Task Obtener_Inexistente_Responde404ConCodigo()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri($"/{int.MaxValue}"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problema.GetProperty("codigo").GetString().ShouldBe("EntidadAcademica.NoEncontrado");
    }

    [Fact]
    public async Task Obtener_DadaDeBaja_Responde404()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var (id, _) = await CrearYObtener(cliente, areaId);
        (await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await cliente.GetAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Modificar_Responde204YPermiteCambiarElArea()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var otraAreaId = await CrearArea(cliente);
        var (id, _) = await CrearYObtener(cliente, areaId);

        using var respuesta = await cliente.PutAsJsonAsync(
            Uri($"/{id}"),
            new
            {
                nombre = "Facultad Modificada",
                calle = "Nueva calle",
                numeroExterior = (string?)null,
                colonia = "Nueva colonia",
                codigoPostal = "91021",
                telefono = "2288421701",
                extension = (string?)null,
                areaAcademicaId = otraAreaId,
                municipioId = MunicipioId,
            },
            Cancelacion);
        using var consulta = await cliente.GetAsync(Uri($"/{id}"), Cancelacion);
        var entidad = await Leer(consulta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        entidad.GetProperty("nombre").GetString().ShouldBe("Facultad Modificada");
        entidad.GetProperty("areaAcademica").GetProperty("id").GetInt32().ShouldBe(otraAreaId);
    }

    [Fact]
    public async Task Modificar_Inexistente_Responde404()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);

        using var respuesta = await cliente.PutAsJsonAsync(
            Uri($"/{int.MaxValue}"),
            new
            {
                nombre = "Facultad",
                calle = "Calle",
                numeroExterior = (string?)null,
                colonia = "Colonia",
                codigoPostal = "91020",
                telefono = "2288421700",
                extension = (string?)null,
                areaAcademicaId = areaId,
                municipioId = MunicipioId,
            },
            Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Modificar_ConAreaAcademicaInexistente_Responde400ConErrorEnAreaAcademicaId()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var (id, _) = await CrearYObtener(cliente, areaId);

        using var respuesta = await cliente.PutAsJsonAsync(
            Uri($"/{id}"),
            new
            {
                nombre = "Facultad",
                calle = "Calle",
                numeroExterior = (string?)null,
                colonia = "Colonia",
                codigoPostal = "91020",
                telefono = "2288421700",
                extension = (string?)null,
                areaAcademicaId = 999,
                municipioId = MunicipioId,
            },
            Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty("areaAcademicaId", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Modificar_ConMunicipioInexistente_Responde400ConErrorEnMunicipioId()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var (id, _) = await CrearYObtener(cliente, areaId);

        using var respuesta = await cliente.PutAsJsonAsync(
            Uri($"/{id}"),
            new
            {
                nombre = "Facultad",
                calle = "Calle",
                numeroExterior = (string?)null,
                colonia = "Colonia",
                codigoPostal = "91020",
                telefono = "2288421700",
                extension = (string?)null,
                areaAcademicaId = areaId,
                municipioId = 999,
            },
            Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty("municipioId", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task DarDeBaja_Activa_Responde204()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var (id, _) = await CrearYObtener(cliente, areaId);

        using var respuesta = await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DarDeBaja_YaDadaDeBaja_Responde404()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var (id, _) = await CrearYObtener(cliente, areaId);
        (await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DarDeBaja_ConUsuariosActivos_Responde409()
    {
        using var clienteAdmin = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(clienteAdmin);
        var (id, _) = await CrearYObtener(clienteAdmin, areaId);
        await CrearCuentaEntidadAcademica(clienteAdmin, id);

        using var respuesta = await clienteAdmin.DeleteAsync(Uri($"/{id}"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("EntidadAcademica.TieneUsuariosActivos");
    }

    [Fact]
    public async Task DarDeBaja_SiSusUsuariosEstanDadosDeBaja_Responde204()
    {
        using var clienteAdmin = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(clienteAdmin);
        var (id, _) = await CrearYObtener(clienteAdmin, areaId);
        var cuentaId = await CrearCuentaEntidadAcademica(clienteAdmin, id);
        (await clienteAdmin.DeleteAsync(new Uri($"/api/v1/usuarios/cuentas/{cuentaId}", UriKind.Relative), Cancelacion))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await clienteAdmin.DeleteAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DarDeBaja_ConProgramasActivos_Responde409()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var (id, _) = await CrearYObtener(cliente, areaId);
        using var dgaa = await _api.CrearClienteDgaaAsync(areaId);
        await CrearPrograma(dgaa, id);

        using var respuesta = await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("EntidadAcademica.TieneProgramasActivos");
    }

    [Fact]
    public async Task DarDeBaja_ConProgramasDadosDeBaja_Responde204()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var (id, _) = await CrearYObtener(cliente, areaId);
        using var dgaa = await _api.CrearClienteDgaaAsync(areaId);
        await DarDeBajaPrograma(dgaa, await CrearPrograma(dgaa, id));

        using var respuesta = await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Modificar_CambiandoAreaConProgramas_Responde409(bool programaDadoDeBaja)
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var otraAreaId = await CrearArea(cliente);
        var (id, _) = await CrearYObtener(cliente, areaId);
        using var dgaa = await _api.CrearClienteDgaaAsync(areaId);
        var programaId = await CrearPrograma(dgaa, id);
        if (programaDadoDeBaja)
        {
            await DarDeBajaPrograma(dgaa, programaId);
        }

        using var respuesta = await cliente.PutAsJsonAsync(Uri($"/{id}"), CuerpoDeModificacion(otraAreaId), Cancelacion);
        var problema = await Leer(respuesta);
        using var consulta = await cliente.GetAsync(Uri($"/{id}"), Cancelacion);
        var entidad = await Leer(consulta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("EntidadAcademica.AreaAcademicaInmutable");
        entidad.GetProperty("areaAcademica").GetProperty("id").GetInt32().ShouldBe(areaId);
        entidad.GetProperty("nombre").GetString().ShouldBe("Facultad de Letras");
    }

    [Fact]
    public async Task Modificar_SinCambiarAreaConProgramas_Responde204()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var (id, _) = await CrearYObtener(cliente, areaId);
        using var dgaa = await _api.CrearClienteDgaaAsync(areaId);
        await CrearPrograma(dgaa, id);

        using var respuesta = await cliente.PutAsJsonAsync(Uri($"/{id}"), CuerpoDeModificacion(areaId), Cancelacion);
        using var consulta = await cliente.GetAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Leer(consulta)).GetProperty("nombre").GetString().ShouldBe("Facultad Modificada");
    }

    [Fact]
    public async Task Listar_FiltraPorAreaAcademicaYExcluyeLasDadasDeBaja()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var (idActiva, _) = await CrearYObtener(cliente, areaId);
        var (idBaja, _) = await CrearYObtener(cliente, areaId);
        (await cliente.DeleteAsync(Uri($"/{idBaja}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await cliente.GetAsync(Uri($"?areaAcademicaId={areaId}"), Cancelacion);
        var pagina = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var ids = pagina.GetProperty("elementos").EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToList();
        ids.ShouldBe([idActiva]);
        pagina.GetProperty("total").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Listar_ConBusquedaIgnoraMayusculasYAcentosYBuscaEnClaveYNombre()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var cuerpo = CuerpoValido(areaId);
        cuerpo["nombre"] = "Facultad de Música";
        using var creada = await cliente.PostAsJsonAsync(Uri(), cuerpo, Cancelacion);
        creada.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await Leer(creada)).GetProperty("id").GetInt32();

        using var respuesta = await cliente.GetAsync(Uri($"?areaAcademicaId={areaId}&busqueda=musica"), Cancelacion);
        var pagina = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        pagina.GetProperty("elementos").EnumerateArray()
            .Select(e => e.GetProperty("id").GetInt32()).ShouldBe([id]);
    }

    [Fact]
    public async Task Listar_ConCalleColoniaCodigoPostalYTelefono_FiltraPorCadaUno()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        var cuerpo = CuerpoValido(areaId);
        cuerpo["calle"] = "Avenida Xalapa Única";
        cuerpo["colonia"] = "Colonia Única de Prueba";
        cuerpo["codigoPostal"] = "91099";
        cuerpo["telefono"] = "2280000099";
        using var creada = await cliente.PostAsJsonAsync(Uri(), cuerpo, Cancelacion);
        var id = (await Leer(creada)).GetProperty("id").GetInt32();

        await VerificaFiltro(cliente, $"areaAcademicaId={areaId}&calle=xalapa", id);
        await VerificaFiltro(cliente, $"areaAcademicaId={areaId}&colonia={System.Uri.EscapeDataString("unica de prueba")}", id);
        await VerificaFiltro(cliente, $"areaAcademicaId={areaId}&codigoPostal=91099", id);
        await VerificaFiltro(cliente, $"areaAcademicaId={areaId}&telefono=2280000099", id);
        await VerificaFiltro(cliente, $"areaAcademicaId={areaId}&campusId={CampusId}&municipioId={MunicipioId}", id);
    }

    [Fact]
    public async Task Listar_ConPaginacionInvalida_Responde400()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var pagina0 = await cliente.GetAsync(Uri("?pagina=0"), Cancelacion);
        using var tamanoExcesivo = await cliente.GetAsync(Uri("?tamanoPagina=101"), Cancelacion);

        pagina0.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        tamanoExcesivo.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Listar_ConTamanoPaginaPersonalizado_DevuelveElTotalYLaPaginaCorrectos()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearArea(cliente);
        await CrearYObtener(cliente, areaId);
        await CrearYObtener(cliente, areaId);
        await CrearYObtener(cliente, areaId);

        using var respuesta = await cliente.GetAsync(
            Uri($"?areaAcademicaId={areaId}&pagina=1&tamanoPagina=2"), Cancelacion);
        var pagina = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        pagina.GetProperty("total").GetInt32().ShouldBe(3);
        pagina.GetProperty("tamanoPagina").GetInt32().ShouldBe(2);
        pagina.GetProperty("elementos").GetArrayLength().ShouldBe(2);
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static Uri Uri(string sufijo = "") => new($"{Ruta}{sufijo}", UriKind.Relative);

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>(Cancelacion);

    private static JsonObject CuerpoValido(int areaAcademicaId, string? clave = null) => new()
    {
        ["clave"] = clave ?? DatosUnicos.ClaveAlfanumerica(),
        ["nombre"] = "Facultad de Letras",
        ["calle"] = "Francisco Moreno",
        ["numeroExterior"] = null,
        ["colonia"] = "Ferrer Guardia",
        ["codigoPostal"] = "91020",
        ["telefono"] = "2288421700",
        ["extension"] = null,
        ["campusId"] = CampusId,
        ["areaAcademicaId"] = areaAcademicaId,
        ["municipioId"] = MunicipioId,
    };

    private static async Task<int> CrearArea(HttpClient cliente)
    {
        using var respuesta = await cliente.PostAsJsonAsync(
            new Uri(RutaAreas, UriKind.Relative),
            new { clave = DatosUnicos.ClaveEntera(), nombre = "Área de prueba", telefono = "2288421700", extension = (string?)null },
            Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await Leer(respuesta)).GetProperty("id").GetInt32();
    }

    private static async Task<string> Crear(HttpClient cliente, int areaAcademicaId)
    {
        var (_, clave) = await CrearYObtener(cliente, areaAcademicaId);
        return clave;
    }

    private static object CuerpoDeModificacion(int areaAcademicaId) => new
    {
        nombre = "Facultad Modificada",
        calle = "Nueva calle",
        numeroExterior = (string?)null,
        colonia = "Nueva colonia",
        codigoPostal = "91021",
        telefono = "2288421701",
        extension = (string?)null,
        areaAcademicaId,
        municipioId = MunicipioId,
    };

    /// <summary>Crea un programa por la API con el DGAA del área de la entidad (sistema 1 y nivel 3 de la semilla).</summary>
    private static async Task<int> CrearPrograma(HttpClient dgaa, int entidadAcademicaId)
    {
        using var respuesta = await dgaa.PostAsJsonAsync(
            new Uri(RutaProgramas, UriKind.Relative),
            new
            {
                nombre = DatosUnicos.Nombre("Programa"),
                entidadAcademicaId,
                sistemaEducativoId = 1,
                nivelFormacionId = 3,
            },
            Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await Leer(respuesta)).GetProperty("id").GetInt32();
    }

    private static async Task DarDeBajaPrograma(HttpClient dgaa, int programaId) =>
        (await dgaa.DeleteAsync(new Uri($"{RutaProgramas}/{programaId}", UriKind.Relative), Cancelacion))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

    private static async Task<int> CrearCuentaEntidadAcademica(HttpClient cliente, int entidadAcademicaId)
    {
        using var respuesta = await cliente.PostAsJsonAsync(
            new Uri("/api/v1/usuarios/cuentas", UriKind.Relative),
            new
            {
                correo = DatosUnicos.Correo("uv.mx"),
                nombre = "Entidad de prueba",
                rolId = 3,
                areaAcademicaId = (int?)null,
                entidadAcademicaId,
            },
            Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await Leer(respuesta)).GetProperty("cuenta").GetProperty("id").GetInt32();
    }

    private static async Task<(int Id, string Clave)> CrearYObtener(HttpClient cliente, int areaAcademicaId)
    {
        var cuerpo = CuerpoValido(areaAcademicaId);
        using var respuesta = await cliente.PostAsJsonAsync(Uri(), cuerpo, Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var creada = await Leer(respuesta);

        return (creada.GetProperty("id").GetInt32(), creada.GetProperty("clave").GetString()!);
    }

    private static async Task VerificaFiltro(HttpClient cliente, string queryString, int idEsperado)
    {
        using var respuesta = await cliente.GetAsync(Uri($"?{queryString}"), Cancelacion);
        var pagina = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        pagina.GetProperty("elementos").EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ShouldBe([idEsperado]);
    }
}
