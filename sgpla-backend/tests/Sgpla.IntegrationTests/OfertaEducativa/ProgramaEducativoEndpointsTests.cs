using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests.OfertaEducativa;

public sealed class ProgramaEducativoEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private const string Ruta = "/api/v1/oferta-educativa/programas-educativos";

    // Semilla: sistemas educativos 1 (Escolarizada) y 2, niveles de formación 3 (LIC) y 4.
    private const int SistemaId = 1;
    private const int OtroSistemaId = 2;
    private const int NivelId = 3;
    private const int OtroNivelId = 4;

    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Crear_ConDatosValidos_Responde201ConLaRespuestaAnidada()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearAreaAsync(superusuario);
        var entidad = await CrearEntidadAsync(superusuario, areaId);
        using var dgaa = await _api.CrearClienteDgaaAsync(areaId);

        using var respuesta = await dgaa.PostAsJsonAsync(
            Uri(), Cuerpo("  Ingeniería   de  Software ", entidad.Id), Cancelacion);
        var creado = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = creado.GetProperty("id").GetInt32();
        creado.GetProperty("nombre").GetString().ShouldBe("Ingeniería de Software");
        creado.GetProperty("entidadAcademica").GetProperty("id").GetInt32().ShouldBe(entidad.Id);
        creado.GetProperty("entidadAcademica").GetProperty("clave").GetString().ShouldBe(entidad.Clave);
        creado.GetProperty("entidadAcademica").GetProperty("nombre").GetString().ShouldBe(entidad.Nombre);
        creado.GetProperty("sistemaEducativo").GetProperty("id").GetInt32().ShouldBe(SistemaId);
        creado.GetProperty("sistemaEducativo").GetProperty("nombre").GetString().ShouldBe("Escolarizada");
        creado.GetProperty("nivelFormacion").GetProperty("id").GetInt32().ShouldBe(NivelId);
        creado.GetProperty("nivelFormacion").GetProperty("clave").GetString().ShouldBe("LIC");
        creado.GetProperty("nivelFormacion").GetProperty("nombre").GetString().ShouldBe("Licenciatura");
        respuesta.Headers.Location.ShouldNotBeNull().AbsolutePath.ShouldBe($"{Ruta}/{id}");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Crear_ConNombreVacio_Responde400ConErrorEnNombre(string nombre)
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;

        using var respuesta = await dgaa.PostAsJsonAsync(Uri(), Cuerpo(nombre, entidad), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "nombre", "ProgramaEducativo.NombreVacio");
    }

    [Fact]
    public async Task Crear_ConNombreDemasiadoLargo_Responde400ConErrorEnNombre()
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;

        using var respuesta = await dgaa.PostAsJsonAsync(Uri(), Cuerpo(new string('A', 201), entidad), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "nombre", "ProgramaEducativo.NombreDemasiadoLargo");
    }

    [Fact]
    public async Task Crear_ConSistemaInexistente_Responde400ConErrorEnSistemaEducativoId()
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;

        using var respuesta = await dgaa.PostAsJsonAsync(Uri(), Cuerpo("Programa", entidad, sistemaId: 999), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "sistemaEducativoId", "ProgramaEducativo.SistemaEducativoInexistente");
    }

    [Fact]
    public async Task Crear_ConNivelInexistente_Responde400ConErrorEnNivelFormacionId()
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;

        using var respuesta = await dgaa.PostAsJsonAsync(Uri(), Cuerpo("Programa", entidad, nivelId: 999), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "nivelFormacionId", "ProgramaEducativo.NivelFormacionInexistente");
    }

    [Fact]
    public async Task Crear_ConEntidadInexistente_Responde400ConErrorEnEntidadAcademicaId()
    {
        var (dgaa, _) = await DgaaConEntidadAsync();
        using var _ = dgaa;

        using var respuesta = await dgaa.PostAsJsonAsync(Uri(), Cuerpo("Programa", int.MaxValue), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "entidadAcademicaId", "ProgramaEducativo.EntidadAcademicaInexistente");
    }

    [Fact]
    public async Task Crear_ConEntidadDadaDeBaja_Responde400ConErrorEnEntidadAcademicaId()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearAreaAsync(superusuario);
        var entidad = await CrearEntidadAsync(superusuario, areaId);
        using var dgaa = await _api.CrearClienteDgaaAsync(areaId);
        (await superusuario.DeleteAsync(RutaEntidad(entidad.Id), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await dgaa.PostAsJsonAsync(Uri(), Cuerpo("Programa", entidad.Id), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "entidadAcademicaId", "ProgramaEducativo.EntidadAcademicaInexistente");
    }

    [Fact]
    public async Task Crear_ConEntidadDeOtraArea_Responde400ConErrorEnEntidadAcademicaId()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearAreaAsync(superusuario);
        var entidadDeOtraArea = await CrearEntidadAsync(superusuario, await CrearAreaAsync(superusuario));
        using var dgaa = await _api.CrearClienteDgaaAsync(areaId);

        using var respuesta = await dgaa.PostAsJsonAsync(Uri(), Cuerpo("Programa", entidadDeOtraArea.Id), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "entidadAcademicaId", "ProgramaEducativo.EntidadAcademicaInexistente");
    }

    [Fact]
    public async Task Crear_ConNombreEquivalenteSinAcentosNiMayusculasEnLaMismaEntidadYSistema_Responde409()
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;
        await CrearProgramaAsync(dgaa, entidad, "Ingeniería de Software");

        using var respuesta = await dgaa.PostAsJsonAsync(Uri(), Cuerpo("INGENIERIA DE SOFTWARE", entidad), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("ProgramaEducativo.NombreDuplicado");
    }

    [Fact]
    public async Task Crear_ConNombreDeUnProgramaDadoDeBaja_Responde409()
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;
        var id = await CrearProgramaAsync(dgaa, entidad, "Ingeniería de Software");
        (await dgaa.DeleteAsync(Uri($"/{id}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await dgaa.PostAsJsonAsync(Uri(), Cuerpo("Ingeniería de Software", entidad), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Crear_ConElMismoNombreEnOtroSistema_Responde201()
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;
        await CrearProgramaAsync(dgaa, entidad, "Ingeniería de Software");

        using var respuesta = await dgaa.PostAsJsonAsync(
            Uri(), Cuerpo("Ingeniería de Software", entidad, sistemaId: OtroSistemaId), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Obtener_Existente_Responde200()
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;
        var id = await CrearProgramaAsync(dgaa, entidad, "Ingeniería de Software");

        using var respuesta = await dgaa.GetAsync(Uri($"/{id}"), Cancelacion);
        var programa = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        programa.GetProperty("id").GetInt32().ShouldBe(id);
        programa.GetProperty("nombre").GetString().ShouldBe("Ingeniería de Software");
    }

    [Fact]
    public async Task Obtener_Inexistente_Responde404ConCodigo()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await superusuario.GetAsync(Uri($"/{int.MaxValue}"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problema.GetProperty("codigo").GetString().ShouldBe("ProgramaEducativo.NoEncontrado");
    }

    [Fact]
    public async Task Obtener_DadoDeBaja_Responde404()
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;
        var id = await CrearProgramaAsync(dgaa, entidad);
        (await dgaa.DeleteAsync(Uri($"/{id}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await dgaa.GetAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Modificar_ConDatosValidos_Responde204YCambiaNombreSistemaYNivel()
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;
        var id = await CrearProgramaAsync(dgaa, entidad, "Ingeniería de Software");

        using var respuesta = await dgaa.PutAsJsonAsync(
            Uri($"/{id}"), Modificacion("  Software   Libre ", OtroSistemaId, OtroNivelId), Cancelacion);
        using var consulta = await dgaa.GetAsync(Uri($"/{id}"), Cancelacion);
        var programa = await Leer(consulta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        programa.GetProperty("nombre").GetString().ShouldBe("Software Libre");
        programa.GetProperty("entidadAcademica").GetProperty("id").GetInt32().ShouldBe(entidad);
        programa.GetProperty("sistemaEducativo").GetProperty("id").GetInt32().ShouldBe(OtroSistemaId);
        programa.GetProperty("nivelFormacion").GetProperty("id").GetInt32().ShouldBe(OtroNivelId);
    }

    [Fact]
    public async Task Modificar_ConSuPropioNombre_Responde204()
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;
        var id = await CrearProgramaAsync(dgaa, entidad, "Ingeniería de Software");

        using var respuesta = await dgaa.PutAsJsonAsync(
            Uri($"/{id}"), Modificacion("INGENIERIA DE SOFTWARE", SistemaId, NivelId), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Modificar_CambiandoLaClasificacionConUnPlan_Responde409(bool planDadoDeBaja)
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;
        var id = await CrearProgramaAsync(dgaa, entidad);
        await DatosAcademicosSql.InsertarPlanAsync(sqlServer.CadenaConexion, id, planDadoDeBaja);

        using var cambiaSistema = await dgaa.PutAsJsonAsync(
            Uri($"/{id}"), Modificacion("Programa", OtroSistemaId, NivelId), Cancelacion);
        using var cambiaNivel = await dgaa.PutAsJsonAsync(
            Uri($"/{id}"), Modificacion("Programa", SistemaId, OtroNivelId), Cancelacion);
        var problema = await Leer(cambiaSistema);

        cambiaSistema.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("ProgramaEducativo.ClasificacionInmutable");
        cambiaNivel.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Modificar_ConUnPlanYSoloElNombre_Responde204()
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;
        var id = await CrearProgramaAsync(dgaa, entidad);
        await DatosAcademicosSql.InsertarPlanAsync(sqlServer.CadenaConexion, id);

        using var respuesta = await dgaa.PutAsJsonAsync(
            Uri($"/{id}"), Modificacion("Otro nombre", SistemaId, NivelId), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Modificar_ConNombreVacio_Responde400ConErrorEnNombre()
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;
        var id = await CrearProgramaAsync(dgaa, entidad);

        using var respuesta = await dgaa.PutAsJsonAsync(Uri($"/{id}"), Modificacion("  ", SistemaId, NivelId), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "nombre", "ProgramaEducativo.NombreVacio");
    }

    [Fact]
    public async Task Modificar_ConSistemaOConNivelInexistente_Responde400ConErrorEnSuCampo()
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;
        var id = await CrearProgramaAsync(dgaa, entidad);

        using var sistema = await dgaa.PutAsJsonAsync(Uri($"/{id}"), Modificacion("Programa", 999, NivelId), Cancelacion);
        using var nivel = await dgaa.PutAsJsonAsync(Uri($"/{id}"), Modificacion("Programa", SistemaId, 999), Cancelacion);

        await VerificaErrorDeCampoAsync(sistema, "sistemaEducativoId", "ProgramaEducativo.SistemaEducativoInexistente");
        await VerificaErrorDeCampoAsync(nivel, "nivelFormacionId", "ProgramaEducativo.NivelFormacionInexistente");
    }

    [Fact]
    public async Task Modificar_ConNombreDeOtroProgramaDeLaEntidadYSistema_Responde409()
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;
        await CrearProgramaAsync(dgaa, entidad, "Software Libre");
        var id = await CrearProgramaAsync(dgaa, entidad, "Ingeniería de Software");

        using var respuesta = await dgaa.PutAsJsonAsync(
            Uri($"/{id}"), Modificacion("SOFTWARE LIBRE", SistemaId, NivelId), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("ProgramaEducativo.NombreDuplicado");
    }

    [Fact]
    public async Task Modificar_Inexistente_Responde404()
    {
        var (dgaa, _) = await DgaaConEntidadAsync();
        using var _ = dgaa;

        using var respuesta = await dgaa.PutAsJsonAsync(
            Uri($"/{int.MaxValue}"), Modificacion("Programa", SistemaId, NivelId), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Modificar_ProgramaDeOtraArea_Responde404()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        var (dgaaDelPrograma, entidad) = await DgaaConEntidadAsync(superusuario);
        using var _ = dgaaDelPrograma;
        var id = await CrearProgramaAsync(dgaaDelPrograma, entidad, "Ingeniería de Software");
        using var dgaaDeOtraArea = await _api.CrearClienteDgaaAsync(await CrearAreaAsync(superusuario));

        using var respuesta = await dgaaDeOtraArea.PutAsJsonAsync(
            Uri($"/{id}"), Modificacion("Otro nombre", SistemaId, NivelId), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problema.GetProperty("codigo").GetString().ShouldBe("ProgramaEducativo.NoEncontrado");
        using var consulta = await dgaaDelPrograma.GetAsync(Uri($"/{id}"), Cancelacion);
        (await Leer(consulta)).GetProperty("nombre").GetString().ShouldBe("Ingeniería de Software");
    }

    [Fact]
    public async Task DarDeBaja_SinPlanes_Responde204YElProgramaDejaDeExistir()
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;
        var id = await CrearProgramaAsync(dgaa, entidad);

        using var respuesta = await dgaa.DeleteAsync(Uri($"/{id}"), Cancelacion);
        using var consulta = await dgaa.GetAsync(Uri($"/{id}"), Cancelacion);
        using var repetida = await dgaa.DeleteAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        consulta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        repetida.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DarDeBaja_ConUnPlanActivo_Responde409()
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;
        var id = await CrearProgramaAsync(dgaa, entidad);
        await DatosAcademicosSql.InsertarPlanAsync(sqlServer.CadenaConexion, id);

        using var respuesta = await dgaa.DeleteAsync(Uri($"/{id}"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("ProgramaEducativo.TienePlanesActivos");
    }

    [Fact]
    public async Task DarDeBaja_ConPlanesDadosDeBaja_Responde204()
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;
        var id = await CrearProgramaAsync(dgaa, entidad);
        await DatosAcademicosSql.InsertarPlanAsync(sqlServer.CadenaConexion, id, dadoDeBaja: true);

        using var respuesta = await dgaa.DeleteAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DarDeBaja_ProgramaDeOtraArea_Responde404()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        var (dgaaDelPrograma, entidad) = await DgaaConEntidadAsync(superusuario);
        using var _ = dgaaDelPrograma;
        var id = await CrearProgramaAsync(dgaaDelPrograma, entidad);
        using var dgaaDeOtraArea = await _api.CrearClienteDgaaAsync(await CrearAreaAsync(superusuario));

        using var respuesta = await dgaaDeOtraArea.DeleteAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        using var consulta = await dgaaDelPrograma.GetAsync(Uri($"/{id}"), Cancelacion);
        consulta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Listar_ComoDgaa_DevuelveSoloLosProgramasDeSuArea()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearAreaAsync(superusuario);
        var entidad1 = (await CrearEntidadAsync(superusuario, areaId)).Id;
        var entidad2 = (await CrearEntidadAsync(superusuario, areaId)).Id;
        using var dgaa = await _api.CrearClienteDgaaAsync(areaId);
        var programa1 = await CrearProgramaAsync(dgaa, entidad1);
        var programa2 = await CrearProgramaAsync(dgaa, entidad2);
        var (dgaaDeOtraArea, entidadDeOtraArea) = await DgaaConEntidadAsync(superusuario);
        using var _ = dgaaDeOtraArea;
        var programaDeOtraArea = await CrearProgramaAsync(dgaaDeOtraArea, entidadDeOtraArea);

        using var respuesta = await dgaa.GetAsync(Uri(), Cancelacion);
        var pagina = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var ids = Ids(pagina);
        ids.ShouldBe([programa1, programa2], ignoreOrder: true);
        ids.ShouldNotContain(programaDeOtraArea);
        pagina.GetProperty("total").GetInt32().ShouldBe(2);
    }

    [Fact]
    public async Task Listar_ComoEntidadAcademica_DevuelveSoloLosProgramasDeSuEntidadYObtenerOtroResponde404()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearAreaAsync(superusuario);
        var entidad1 = (await CrearEntidadAsync(superusuario, areaId)).Id;
        var entidad2 = (await CrearEntidadAsync(superusuario, areaId)).Id;
        using var dgaa = await _api.CrearClienteDgaaAsync(areaId);
        var programaPropio = await CrearProgramaAsync(dgaa, entidad1);
        var programaDeLaOtraEntidad = await CrearProgramaAsync(dgaa, entidad2);
        using var entidadAcademica = await _api.CrearClienteEntidadAcademicaAsync(entidad1);

        using var listado = await entidadAcademica.GetAsync(Uri(), Cancelacion);
        using var propio = await entidadAcademica.GetAsync(Uri($"/{programaPropio}"), Cancelacion);
        using var ajeno = await entidadAcademica.GetAsync(Uri($"/{programaDeLaOtraEntidad}"), Cancelacion);

        listado.StatusCode.ShouldBe(HttpStatusCode.OK);
        Ids(await Leer(listado)).ShouldBe([programaPropio]);
        propio.StatusCode.ShouldBe(HttpStatusCode.OK);
        ajeno.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Listar_ComoSuperusuario_ContieneLosProgramasDeVariasAreas()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        var token = Guid.NewGuid().ToString("N");
        var (dgaaA, entidadA) = await DgaaConEntidadAsync(superusuario);
        using var _ = dgaaA;
        var (dgaaB, entidadB) = await DgaaConEntidadAsync(superusuario);
        using var __ = dgaaB;
        var programaA = await CrearProgramaAsync(dgaaA, entidadA, $"Programa {token} A");
        var programaB = await CrearProgramaAsync(dgaaB, entidadB, $"Programa {token} B");

        using var respuesta = await superusuario.GetAsync(Uri($"?busqueda={token}"), Cancelacion);
        var pagina = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        Ids(pagina).ShouldBe([programaA, programaB], ignoreOrder: true);
        using var obtenidoA = await superusuario.GetAsync(Uri($"/{programaA}"), Cancelacion);
        using var obtenidoB = await superusuario.GetAsync(Uri($"/{programaB}"), Cancelacion);
        obtenidoA.StatusCode.ShouldBe(HttpStatusCode.OK);
        obtenidoB.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Listar_NoIncluyeLosProgramasDadosDeBaja()
    {
        var (dgaa, entidad) = await DgaaConEntidadAsync();
        using var _ = dgaa;
        var activo = await CrearProgramaAsync(dgaa, entidad);
        var dadoDeBaja = await CrearProgramaAsync(dgaa, entidad);
        (await dgaa.DeleteAsync(Uri($"/{dadoDeBaja}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await dgaa.GetAsync(Uri(), Cancelacion);

        Ids(await Leer(respuesta)).ShouldBe([activo]);
    }

    [Fact]
    public async Task Listar_ConFiltros_CombinaCadaUnoConAnd()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearAreaAsync(superusuario);
        var entidad1 = (await CrearEntidadAsync(superusuario, areaId)).Id;
        var entidad2 = (await CrearEntidadAsync(superusuario, areaId)).Id;
        using var dgaa = await _api.CrearClienteDgaaAsync(areaId);
        var software = await CrearProgramaAsync(dgaa, entidad1, "Ingeniería de Software");
        var musica = await CrearProgramaAsync(dgaa, entidad1, "Música Popular", OtroSistemaId, OtroNivelId);
        var derecho = await CrearProgramaAsync(dgaa, entidad2, "Derecho");

        await VerificaFiltroAsync(dgaa, $"entidadAcademicaId={entidad1}", software, musica);
        await VerificaFiltroAsync(dgaa, $"entidadAcademicaId={entidad2}", derecho);
        await VerificaFiltroAsync(dgaa, $"sistemaEducativoId={OtroSistemaId}", musica);
        await VerificaFiltroAsync(dgaa, $"nivelFormacionId={OtroNivelId}", musica);
        await VerificaFiltroAsync(dgaa, $"entidadAcademicaId={entidad1}&nivelFormacionId={NivelId}", software);
        await VerificaFiltroAsync(dgaa, "busqueda=musica", musica);
        await VerificaFiltroAsync(dgaa, $"busqueda={System.Uri.EscapeDataString("  INGENIERIA   de  software ")}", software);
        await VerificaFiltroAsync(dgaa, "busqueda=%20%20", software, musica, derecho);
        await VerificaFiltroAsync(dgaa, $"entidadAcademicaId={entidad2}&sistemaEducativoId={OtroSistemaId}");
    }

    [Fact]
    public async Task Listar_ConPaginacion_DevuelveElTotalYLaPaginaEnOrdenDeNombre()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearAreaAsync(superusuario);
        var entidad = (await CrearEntidadAsync(superusuario, areaId)).Id;
        using var dgaa = await _api.CrearClienteDgaaAsync(areaId);
        var derecho = await CrearProgramaAsync(dgaa, entidad, "Derecho");
        var musica = await CrearProgramaAsync(dgaa, entidad, "Música");
        var software = await CrearProgramaAsync(dgaa, entidad, "Ingeniería de Software");

        using var primera = await dgaa.GetAsync(Uri("?pagina=1&tamanoPagina=2"), Cancelacion);
        using var segunda = await dgaa.GetAsync(Uri("?pagina=2&tamanoPagina=2"), Cancelacion);
        var paginaUno = await Leer(primera);
        var paginaDos = await Leer(segunda);

        paginaUno.GetProperty("total").GetInt32().ShouldBe(3);
        paginaUno.GetProperty("tamanoPagina").GetInt32().ShouldBe(2);
        Ids(paginaUno).ShouldBe([derecho, software]);
        paginaDos.GetProperty("pagina").GetInt32().ShouldBe(2);
        Ids(paginaDos).ShouldBe([musica]);
    }

    [Theory]
    [InlineData("pagina=0")]
    [InlineData("tamanoPagina=101")]
    [InlineData("entidadAcademicaId=0")]
    [InlineData("sistemaEducativoId=0")]
    [InlineData("nivelFormacionId=0")]
    public async Task Listar_ConParametrosInvalidos_Responde400(string consulta)
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await superusuario.GetAsync(Uri($"?{consulta}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Escribir_ComoSuperusuario_Responde403()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        var (dgaa, entidad) = await DgaaConEntidadAsync(superusuario);
        using var _ = dgaa;
        var id = await CrearProgramaAsync(dgaa, entidad);

        await VerificaEscrituraProhibidaAsync(superusuario, entidad, id);
    }

    [Fact]
    public async Task Escribir_ComoEntidadAcademica_Responde403()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        var (dgaa, entidad) = await DgaaConEntidadAsync(superusuario);
        using var _ = dgaa;
        var id = await CrearProgramaAsync(dgaa, entidad);
        using var entidadAcademica = await _api.CrearClienteEntidadAcademicaAsync(entidad);

        await VerificaEscrituraProhibidaAsync(entidadAcademica, entidad, id);
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static Uri Uri(string sufijo = "") => new($"{Ruta}{sufijo}", UriKind.Relative);

    private static Uri RutaEntidad(int id) => new($"/api/v1/institucional/entidades-academicas/{id}", UriKind.Relative);

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>(Cancelacion);

    private static List<int> Ids(JsonElement pagina) =>
        pagina.GetProperty("elementos").EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToList();

    private static object Cuerpo(string nombre, int entidadAcademicaId, int sistemaId = SistemaId, int nivelId = NivelId) =>
        new { nombre, entidadAcademicaId, sistemaEducativoId = sistemaId, nivelFormacionId = nivelId };

    private static object Modificacion(string nombre, int sistemaId, int nivelId) =>
        new { nombre, sistemaEducativoId = sistemaId, nivelFormacionId = nivelId };

    private static async Task VerificaErrorDeCampoAsync(HttpResponseMessage respuesta, string campo, string codigo)
    {
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty(campo, out _).ShouldBeTrue();
        problema.GetProperty("codigo").GetString().ShouldBe(codigo);
    }

    /// <summary>Con los filtros dados, el DGAA de una área de la prueba ve exactamente los programas esperados.</summary>
    private static async Task VerificaFiltroAsync(HttpClient dgaa, string consulta, params int[] idsEsperados)
    {
        using var respuesta = await dgaa.GetAsync(Uri($"?{consulta}"), Cancelacion);
        var pagina = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        Ids(pagina).ShouldBe(idsEsperados, ignoreOrder: true);
    }

    private static async Task VerificaEscrituraProhibidaAsync(HttpClient cliente, int entidadId, int programaId)
    {
        using var crear = await cliente.PostAsJsonAsync(Uri(), Cuerpo("Programa", entidadId), Cancelacion);
        using var modificar = await cliente.PutAsJsonAsync(
            Uri($"/{programaId}"), Modificacion("Otro nombre", SistemaId, NivelId), Cancelacion);
        using var darDeBaja = await cliente.DeleteAsync(Uri($"/{programaId}"), Cancelacion);

        crear.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        modificar.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        darDeBaja.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>Crea un área nueva con una entidad y devuelve el cliente de su DGAA junto con el id de la entidad.</summary>
    private async Task<(HttpClient Dgaa, int EntidadId)> DgaaConEntidadAsync(HttpClient? superusuario = null)
    {
        var propio = superusuario is null;
        superusuario ??= await _api.CrearClienteSuperusuarioAsync();
        try
        {
            var areaId = await CrearAreaAsync(superusuario);
            var entidad = await CrearEntidadAsync(superusuario, areaId);
            return (await _api.CrearClienteDgaaAsync(areaId), entidad.Id);
        }
        finally
        {
            if (propio)
            {
                superusuario.Dispose();
            }
        }
    }

    private static async Task<int> CrearProgramaAsync(
        HttpClient dgaa,
        int entidadAcademicaId,
        string? nombre = null,
        int sistemaId = SistemaId,
        int nivelId = NivelId)
    {
        using var respuesta = await dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(nombre ?? DatosUnicos.Nombre("Programa"), entidadAcademicaId, sistemaId, nivelId), Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await Leer(respuesta)).GetProperty("id").GetInt32();
    }

    private static async Task<int> CrearAreaAsync(HttpClient superusuario)
    {
        using var respuesta = await superusuario.PostAsJsonAsync(
            new Uri("/api/v1/institucional/areas-academicas", UriKind.Relative),
            new { clave = DatosUnicos.ClaveEntera(), nombre = "Área de prueba", telefono = "2288421700", extension = (string?)null },
            Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await Leer(respuesta)).GetProperty("id").GetInt32();
    }

    private static async Task<(int Id, string Clave, string Nombre)> CrearEntidadAsync(HttpClient superusuario, int areaAcademicaId)
    {
        var nombre = DatosUnicos.Nombre("Facultad");
        using var respuesta = await superusuario.PostAsJsonAsync(
            new Uri("/api/v1/institucional/entidades-academicas", UriKind.Relative),
            new
            {
                clave = DatosUnicos.ClaveAlfanumerica(),
                nombre,
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
        var creada = await Leer(respuesta);

        return (creada.GetProperty("id").GetInt32(), creada.GetProperty("clave").GetString()!, nombre);
    }
}
