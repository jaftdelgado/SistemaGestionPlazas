using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests.OfertaEducativa;

public sealed class ProgramacionAcademicaEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private const string Ruta = "/api/v1/oferta-educativa/programaciones-academicas";

    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Listar_ConCadaFiltro_DevuelveSoloLasProgramacionesEsperadas()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        using var otraEntidad = await EscenarioOferta.CrearAsync(_api, superusuario);
        var (otroProgramaId, _) = await EscenarioOferta.CrearProgramaAsync(escenario.Dgaa, escenario.EntidadId);
        var planA = await CrearPlanAsync(escenario.Dgaa, escenario.ProgramaId, 2);
        var planB = await CrearPlanAsync(escenario.Dgaa, otroProgramaId, 1);
        var planC = await CrearPlanAsync(otraEntidad.Dgaa, otraEntidad.ProgramaId, 1);
        var periodo1 = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        var periodo2 = await EscenarioOferta.CrearPeriodoAsync(superusuario);

        var pr1 = await InsertarAsync(periodo1, planA.Experiencias[0].Id, "NRC001");
        var pr2 = await InsertarAsync(periodo1, planA.Experiencias[1].Id, "NRC002");
        var pr3 = await InsertarAsync(periodo2, planB.Experiencias[0].Id, "NRC003");
        var pr4 = await InsertarAsync(periodo1, planC.Experiencias[0].Id, "NRC004");
        var entidad = $"entidadAcademicaId={escenario.EntidadId}";

        await VerificaFiltroAsync(superusuario, entidad, pr3, pr1, pr2);
        await VerificaFiltroAsync(superusuario, $"entidadAcademicaId={otraEntidad.EntidadId}", pr4);
        await VerificaFiltroAsync(superusuario, $"{entidad}&periodoEscolarId={periodo1}", pr1, pr2);
        await VerificaFiltroAsync(superusuario, $"{entidad}&periodoEscolarId={periodo2}", pr3);
        await VerificaFiltroAsync(superusuario, $"programaEducativoId={escenario.ProgramaId}", pr1, pr2);
        await VerificaFiltroAsync(superusuario, $"programaEducativoId={otroProgramaId}", pr3);
        await VerificaFiltroAsync(superusuario, $"planEstudiosId={planA.Id}", pr1, pr2);
        await VerificaFiltroAsync(superusuario, $"planEstudiosId={planB.Id}", pr3);
        await VerificaFiltroAsync(superusuario, $"experienciaEducativaId={planA.Experiencias[1].Id}", pr2);
        await VerificaFiltroAsync(superusuario, $"{entidad}&nrc=NRC003", pr3);
        await VerificaFiltroAsync(superusuario, $"{entidad}&periodoEscolarId={periodo1}&planEstudiosId={planB.Id}");
        await VerificaFiltroAsync(superusuario, $"{entidad}&periodoEscolarId={periodo1}&nrc=NRC002&planEstudiosId={planA.Id}", pr2);
    }

    [Fact]
    public async Task Listar_ConNrcEnMinusculasOConEspacios_EncuentraElMismoRegistro()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var plan = await CrearPlanAsync(escenario.Dgaa, escenario.ProgramaId, 2);
        var periodo = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        var buscada = await InsertarAsync(periodo, plan.Experiencias[0].Id, "A1B2C3");
        var otra = await InsertarAsync(periodo, plan.Experiencias[1].Id, "A1B2C4");
        var entidad = $"entidadAcademicaId={escenario.EntidadId}";

        await VerificaFiltroAsync(superusuario, $"{entidad}&nrc=a1b2c3", buscada);
        await VerificaFiltroAsync(superusuario, $"{entidad}&nrc=%20%20a1b2c3%20%20", buscada);
        await VerificaFiltroAsync(superusuario, $"{entidad}&nrc=A1B2C3", buscada);

        // Exacto, no por fragmentos; y un NRC solo con espacios se ignora como cualquier filtro de texto vacío.
        await VerificaFiltroAsync(superusuario, $"{entidad}&nrc=A1B2");
        await VerificaFiltroAsync(superusuario, $"{entidad}&nrc=%20%20", buscada, otra);
        await VerificaFiltroAsync(superusuario, $"{entidad}&nrc=", buscada, otra);
    }

    [Fact]
    public async Task Listar_OrdenaPorClaveDePeriodoDescendenteYNrc()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var plan = await CrearPlanAsync(escenario.Dgaa, escenario.ProgramaId, 3);
        var anterior = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        var reciente = await EscenarioOferta.CrearPeriodoAsync(superusuario);

        var anteriorB = await InsertarAsync(anterior, plan.Experiencias[0].Id, "NRCB");
        var anteriorA = await InsertarAsync(anterior, plan.Experiencias[1].Id, "NRCA");
        var recienteC = await InsertarAsync(reciente, plan.Experiencias[2].Id, "NRCC");

        using var respuesta = await superusuario.GetAsync(Uri($"?planEstudiosId={plan.Id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        Ids(await EscenarioOferta.Leer(respuesta)).ShouldBe([recienteC, anteriorA, anteriorB]);
    }

    [Fact]
    public async Task Listar_ConPaginacion_DevuelveLaPaginaPedidaYElTotal()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var plan = await CrearPlanAsync(escenario.Dgaa, escenario.ProgramaId, 3);
        var periodo = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        var primera = await InsertarAsync(periodo, plan.Experiencias[0].Id, "NRC001");
        var segunda = await InsertarAsync(periodo, plan.Experiencias[1].Id, "NRC002");
        var tercera = await InsertarAsync(periodo, plan.Experiencias[2].Id, "NRC003");

        using var respuestaUno = await superusuario.GetAsync(Uri($"?planEstudiosId={plan.Id}&pagina=1&tamanoPagina=2"), Cancelacion);
        using var respuestaDos = await superusuario.GetAsync(Uri($"?planEstudiosId={plan.Id}&pagina=2&tamanoPagina=2"), Cancelacion);
        var paginaUno = await EscenarioOferta.Leer(respuestaUno);
        var paginaDos = await EscenarioOferta.Leer(respuestaDos);

        Ids(paginaUno).ShouldBe([primera, segunda]);
        Ids(paginaDos).ShouldBe([tercera]);
        paginaUno.GetProperty("total").GetInt32().ShouldBe(3);
        paginaDos.GetProperty("tamanoPagina").GetInt32().ShouldBe(2);
        paginaDos.GetProperty("pagina").GetInt32().ShouldBe(2);
    }

    [Theory]
    [InlineData("pagina=0", "pagina")]
    [InlineData("tamanoPagina=101", "tamanoPagina")]
    [InlineData("periodoEscolarId=0", "periodoEscolarId")]
    [InlineData("entidadAcademicaId=-1", "entidadAcademicaId")]
    [InlineData("programaEducativoId=0", "programaEducativoId")]
    [InlineData("planEstudiosId=0", "planEstudiosId")]
    [InlineData("experienciaEducativaId=0", "experienciaEducativaId")]
    [InlineData("nrc=012345678901234567890", "nrc")]
    public async Task Listar_ConParametrosInvalidos_Responde400ConErrorEnElParametro(string consulta, string campo)
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await superusuario.GetAsync(Uri($"?{consulta}"), Cancelacion);
        var problema = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty(campo, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Listar_NoIncluyeLasProgramacionesDadasDeBaja()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var plan = await CrearPlanAsync(escenario.Dgaa, escenario.ProgramaId, 2);
        var periodo = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        var activa = await InsertarAsync(periodo, plan.Experiencias[0].Id, "NRC001");
        await InsertarAsync(periodo, plan.Experiencias[1].Id, "NRC002", dadaDeBaja: true);

        await VerificaFiltroAsync(superusuario, $"planEstudiosId={plan.Id}", activa);
    }

    [Fact]
    public async Task Obtener_Responde200ConLaRespuestaAnidada()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var plan = await CrearPlanAsync(escenario.Dgaa, escenario.ProgramaId, 1);
        var periodoId = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        var experiencia = plan.Experiencias[0];
        var id = await InsertarAsync(periodoId, experiencia.Id, "NRC001");

        using var periodo = await superusuario.GetAsync(
            new Uri($"/api/v1/oferta-educativa/periodos-escolares/{periodoId}", UriKind.Relative), Cancelacion);
        var clavePeriodo = (await EscenarioOferta.Leer(periodo)).GetProperty("clave").GetString();

        using var respuesta = await superusuario.GetAsync(Uri($"/{id}"), Cancelacion);
        var programacion = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        programacion.GetProperty("id").GetInt32().ShouldBe(id);
        programacion.GetProperty("nrc").GetString().ShouldBe("NRC001");
        programacion.GetProperty("periodoEscolar").GetProperty("id").GetInt32().ShouldBe(periodoId);
        programacion.GetProperty("periodoEscolar").GetProperty("clave").GetString().ShouldBe(clavePeriodo);
        programacion.GetProperty("experienciaEducativa").GetProperty("id").GetInt32().ShouldBe(experiencia.Id);
        programacion.GetProperty("experienciaEducativa").GetProperty("materia").GetString().ShouldBe(experiencia.Materia);
        programacion.GetProperty("experienciaEducativa").GetProperty("curso").GetString().ShouldBe("00001");
        programacion.GetProperty("experienciaEducativa").GetProperty("nombre").GetString().ShouldBe(experiencia.Nombre);
        programacion.GetProperty("planEstudios").GetProperty("id").GetInt32().ShouldBe(plan.Id);
        programacion.GetProperty("planEstudios").GetProperty("codigo").GetString().ShouldBe(plan.Codigo);
        programacion.GetProperty("programaEducativo").GetProperty("id").GetInt32().ShouldBe(escenario.ProgramaId);
        programacion.GetProperty("programaEducativo").GetProperty("nombre").GetString().ShouldBe(escenario.ProgramaNombre);
        programacion.GetProperty("entidadAcademica").GetProperty("id").GetInt32().ShouldBe(escenario.EntidadId);
        programacion.GetProperty("entidadAcademica").GetProperty("clave").GetString().ShouldBe(escenario.EntidadClave);
        programacion.GetProperty("entidadAcademica").GetProperty("nombre").GetString().ShouldBe(escenario.EntidadNombre);
    }

    [Fact]
    public async Task Obtener_DeUnaProgramacionDadaDeBaja_Responde404()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var plan = await CrearPlanAsync(escenario.Dgaa, escenario.ProgramaId, 1);
        var periodo = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        var id = await InsertarAsync(periodo, plan.Experiencias[0].Id, "NRC001", dadaDeBaja: true);

        using var respuesta = await superusuario.GetAsync(Uri($"/{id}"), Cancelacion);

        await VerificaNoEncontradoAsync(respuesta);
    }

    [Fact]
    public async Task Obtener_DeUnaProgramacionInexistente_Responde404()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await superusuario.GetAsync(Uri($"/{int.MaxValue}"), Cancelacion);

        await VerificaNoEncontradoAsync(respuesta);
    }

    [Fact]
    public async Task Ambito_UnDgaaSoloVeLasProgramacionesDeSuArea_YRecibe404ConLasAjenas()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var propio = await EscenarioOferta.CrearAsync(_api, superusuario);
        using var ajeno = await EscenarioOferta.CrearAsync(_api, superusuario);
        var periodo = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        var planPropio = await CrearPlanAsync(propio.Dgaa, propio.ProgramaId, 1);
        var planAjeno = await CrearPlanAsync(ajeno.Dgaa, ajeno.ProgramaId, 1);
        var programacionPropia = await InsertarAsync(periodo, planPropio.Experiencias[0].Id, "NRC001");
        var programacionAjena = await InsertarAsync(periodo, planAjeno.Experiencias[0].Id, "NRC002");

        using var delPropio = await propio.Dgaa.GetAsync(Uri($"/{programacionPropia}"), Cancelacion);
        using var delAjeno = await propio.Dgaa.GetAsync(Uri($"/{programacionAjena}"), Cancelacion);
        using var lista = await propio.Dgaa.GetAsync(Uri($"?periodoEscolarId={periodo}"), Cancelacion);
        using var listaFiltradaPorLoAjeno = await propio.Dgaa.GetAsync(
            Uri($"?periodoEscolarId={periodo}&entidadAcademicaId={ajeno.EntidadId}"), Cancelacion);

        delPropio.StatusCode.ShouldBe(HttpStatusCode.OK);
        await VerificaNoEncontradoAsync(delAjeno);
        Ids(await EscenarioOferta.Leer(lista)).ShouldBe([programacionPropia]);
        Ids(await EscenarioOferta.Leer(listaFiltradaPorLoAjeno)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Ambito_UnaEntidadAcademicaSoloVeLasProgramacionesDeSuEntidad_YRecibe404ConLasAjenas()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var propio = await EscenarioOferta.CrearAsync(_api, superusuario);
        using var ajeno = await EscenarioOferta.CrearAsync(_api, superusuario);
        using var entidadAcademica = await _api.CrearClienteEntidadAcademicaAsync(propio.EntidadId);
        var periodo = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        var planPropio = await CrearPlanAsync(propio.Dgaa, propio.ProgramaId, 1);
        var planAjeno = await CrearPlanAsync(ajeno.Dgaa, ajeno.ProgramaId, 1);
        var programacionPropia = await InsertarAsync(periodo, planPropio.Experiencias[0].Id, "NRC001");
        var programacionAjena = await InsertarAsync(periodo, planAjeno.Experiencias[0].Id, "NRC002");

        using var delPropio = await entidadAcademica.GetAsync(Uri($"/{programacionPropia}"), Cancelacion);
        using var delAjeno = await entidadAcademica.GetAsync(Uri($"/{programacionAjena}"), Cancelacion);
        using var lista = await entidadAcademica.GetAsync(Uri($"?periodoEscolarId={periodo}"), Cancelacion);

        delPropio.StatusCode.ShouldBe(HttpStatusCode.OK);
        await VerificaNoEncontradoAsync(delAjeno);
        Ids(await EscenarioOferta.Leer(lista)).ShouldBe([programacionPropia]);
    }

    [Fact]
    public async Task Ambito_ElSuperusuarioVeLasProgramacionesDeTodasLasAreas()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var uno = await EscenarioOferta.CrearAsync(_api, superusuario);
        using var otro = await EscenarioOferta.CrearAsync(_api, superusuario);
        var periodo = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        var planUno = await CrearPlanAsync(uno.Dgaa, uno.ProgramaId, 1);
        var planOtro = await CrearPlanAsync(otro.Dgaa, otro.ProgramaId, 1);
        var programacionUno = await InsertarAsync(periodo, planUno.Experiencias[0].Id, "NRC001");
        var programacionOtra = await InsertarAsync(periodo, planOtro.Experiencias[0].Id, "NRC002");

        await VerificaFiltroAsync(superusuario, $"periodoEscolarId={periodo}", programacionUno, programacionOtra);
    }

    [Fact]
    public async Task Escribir_Responde405EnLaColeccionYEnElDetalle()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var plan = await CrearPlanAsync(escenario.Dgaa, escenario.ProgramaId, 1);
        var periodo = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        var id = await InsertarAsync(periodo, plan.Experiencias[0].Id, "NRC001");

        foreach (var (cliente, metodo, sufijo) in new[]
        {
            (superusuario, "POST", ""),
            (escenario.Dgaa, "POST", ""),
            (superusuario, "PUT", $"/{id}"),
            (escenario.Dgaa, "DELETE", $"/{id}"),
        })
        {
            using var solicitud = new HttpRequestMessage(new HttpMethod(metodo), Uri(sufijo))
            {
                Content = JsonContent.Create(new { nrc = "NRC999" }),
            };

            using var respuesta = await cliente.SendAsync(solicitud, Cancelacion);

            respuesta.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed, $"{metodo} {sufijo}");
        }
    }

    [Fact]
    public async Task Horarios_Responde200EnOrdenDeDiaHoraEId_ConEspacioNuloYFormatoDeHorasYFechas()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var plan = await CrearPlanAsync(escenario.Dgaa, escenario.ProgramaId, 1);
        var periodo = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        var programacion = await InsertarAsync(periodo, plan.Experiencias[0].Id, "NRC001");
        var sincronizacion = await DatosAcademicosSql.InsertarSincronizacionAsync(sqlServer.CadenaConexion, periodo);
        var otraSincronizacion = await DatosAcademicosSql.InsertarSincronizacionAsync(sqlServer.CadenaConexion, periodo);
        var inicio = new DateOnly(2026, 8, 10);
        var fin = new DateOnly(2026, 12, 4);

        var miercoles = await InsertarHorarioAsync(programacion, sincronizacion, 3, 8, 10, inicio, fin);
        var lunesTarde = await InsertarHorarioAsync(programacion, sincronizacion, 1, 10, 12, inicio, fin, "Edificio A", "Aula 5");
        var lunesMananaLarga = await InsertarHorarioAsync(programacion, sincronizacion, 1, 8, 10, inicio, fin);
        var lunesMananaCorta = await InsertarHorarioAsync(programacion, sincronizacion, 1, 8, 9, inicio, fin);
        // Sin filtro por fechas ni por sincronización: una sesión de un periodo ya pasado y de otra sincronización también sale.
        var sabado = await InsertarHorarioAsync(
            programacion, otraSincronizacion, 6, 9, 11, new DateOnly(2020, 1, 6), new DateOnly(2020, 5, 1));

        using var respuesta = await superusuario.GetAsync(Uri($"/{programacion}/horarios"), Cancelacion);
        var horarios = (await EscenarioOferta.Leer(respuesta)).EnumerateArray().ToList();

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        horarios.Select(h => h.GetProperty("id").GetInt32())
            .ShouldBe([lunesMananaLarga, lunesMananaCorta, lunesTarde, miercoles, sabado]);

        var primera = horarios[0];
        primera.GetProperty("diaSemana").GetInt32().ShouldBe(1);
        primera.GetProperty("horaInicio").GetString().ShouldBe("08:00:00");
        primera.GetProperty("horaFin").GetString().ShouldBe("10:00:00");
        primera.GetProperty("fechaInicio").GetString().ShouldBe("2026-08-10");
        primera.GetProperty("fechaFin").GetString().ShouldBe("2026-12-04");
        primera.GetProperty("edificio").ValueKind.ShouldBe(JsonValueKind.Null);
        primera.GetProperty("aula").ValueKind.ShouldBe(JsonValueKind.Null);

        var conEspacio = horarios[2];
        conEspacio.GetProperty("horaInicio").GetString().ShouldBe("10:00:00");
        conEspacio.GetProperty("edificio").GetString().ShouldBe("Edificio A");
        conEspacio.GetProperty("aula").GetString().ShouldBe("Aula 5");

        horarios[4].GetProperty("fechaInicio").GetString().ShouldBe("2020-01-06");
    }

    [Fact]
    public async Task Horarios_NoIncluyeLasSesionesDeOtraProgramacion_YSinSesionesRespondeArregloVacio()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var plan = await CrearPlanAsync(escenario.Dgaa, escenario.ProgramaId, 2);
        var periodo = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        var conSesion = await InsertarAsync(periodo, plan.Experiencias[0].Id, "NRC001");
        var sinSesion = await InsertarAsync(periodo, plan.Experiencias[1].Id, "NRC002");
        var sincronizacion = await DatosAcademicosSql.InsertarSincronizacionAsync(sqlServer.CadenaConexion, periodo);
        var sesion = await InsertarHorarioAsync(
            conSesion, sincronizacion, 2, 7, 9, new DateOnly(2026, 8, 10), new DateOnly(2026, 12, 4));

        using var delVacio = await superusuario.GetAsync(Uri($"/{sinSesion}/horarios"), Cancelacion);
        using var delOtro = await superusuario.GetAsync(Uri($"/{conSesion}/horarios"), Cancelacion);

        delVacio.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await EscenarioOferta.Leer(delVacio)).GetArrayLength().ShouldBe(0);
        (await EscenarioOferta.Leer(delOtro)).EnumerateArray().Select(h => h.GetProperty("id").GetInt32()).ShouldBe([sesion]);
    }

    [Fact]
    public async Task Horarios_DeUnaProgramacionDadaDeBajaOInexistente_Responde404()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var plan = await CrearPlanAsync(escenario.Dgaa, escenario.ProgramaId, 1);
        var periodo = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        var dadaDeBaja = await InsertarAsync(periodo, plan.Experiencias[0].Id, "NRC001", dadaDeBaja: true);
        var sincronizacion = await DatosAcademicosSql.InsertarSincronizacionAsync(sqlServer.CadenaConexion, periodo);
        await InsertarHorarioAsync(dadaDeBaja, sincronizacion, 1, 8, 10, new DateOnly(2026, 8, 10), new DateOnly(2026, 12, 4));

        using var deLaBaja = await superusuario.GetAsync(Uri($"/{dadaDeBaja}/horarios"), Cancelacion);
        using var inexistente = await superusuario.GetAsync(Uri($"/{int.MaxValue}/horarios"), Cancelacion);

        await VerificaNoEncontradoAsync(deLaBaja);
        await VerificaNoEncontradoAsync(inexistente);
    }

    [Fact]
    public async Task Horarios_FueraDelAmbito_Responde404_YDentroDelAmbito200()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var propio = await EscenarioOferta.CrearAsync(_api, superusuario);
        using var ajeno = await EscenarioOferta.CrearAsync(_api, superusuario);
        using var entidadAcademica = await _api.CrearClienteEntidadAcademicaAsync(propio.EntidadId);
        var periodo = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        var planPropio = await CrearPlanAsync(propio.Dgaa, propio.ProgramaId, 1);
        var planAjeno = await CrearPlanAsync(ajeno.Dgaa, ajeno.ProgramaId, 1);
        var programacionPropia = await InsertarAsync(periodo, planPropio.Experiencias[0].Id, "NRC001");
        var programacionAjena = await InsertarAsync(periodo, planAjeno.Experiencias[0].Id, "NRC002");
        var sincronizacion = await DatosAcademicosSql.InsertarSincronizacionAsync(sqlServer.CadenaConexion, periodo);
        await InsertarHorarioAsync(programacionPropia, sincronizacion, 1, 8, 10, new DateOnly(2026, 8, 10), new DateOnly(2026, 12, 4));
        await InsertarHorarioAsync(programacionAjena, sincronizacion, 1, 8, 10, new DateOnly(2026, 8, 10), new DateOnly(2026, 12, 4));

        using var dgaaPropio = await propio.Dgaa.GetAsync(Uri($"/{programacionPropia}/horarios"), Cancelacion);
        using var dgaaAjeno = await propio.Dgaa.GetAsync(Uri($"/{programacionAjena}/horarios"), Cancelacion);
        using var entidadPropia = await entidadAcademica.GetAsync(Uri($"/{programacionPropia}/horarios"), Cancelacion);
        using var entidadAjena = await entidadAcademica.GetAsync(Uri($"/{programacionAjena}/horarios"), Cancelacion);
        using var superusuarioAjena = await superusuario.GetAsync(Uri($"/{programacionAjena}/horarios"), Cancelacion);

        dgaaPropio.StatusCode.ShouldBe(HttpStatusCode.OK);
        entidadPropia.StatusCode.ShouldBe(HttpStatusCode.OK);
        superusuarioAjena.StatusCode.ShouldBe(HttpStatusCode.OK);
        await VerificaNoEncontradoAsync(dgaaAjeno);
        await VerificaNoEncontradoAsync(entidadAjena);
    }

    [Fact]
    public async Task Horarios_Escribir_Responde405()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();

        foreach (var metodo in new[] { "POST", "PUT", "DELETE" })
        {
            using var solicitud = new HttpRequestMessage(new HttpMethod(metodo), Uri("/1/horarios"))
            {
                Content = JsonContent.Create(new { diaSemana = 1 }),
            };

            using var respuesta = await superusuario.SendAsync(solicitud, Cancelacion);

            respuesta.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed, metodo);
        }
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static Uri Uri(string sufijo = "") => new($"{Ruta}{sufijo}", UriKind.Relative);

    private static List<int> Ids(JsonElement pagina) =>
        pagina.GetProperty("elementos").EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToList();

    /// <summary>Importa un plan con <paramref name="cantidad"/> EE y devuelve sus ids, en orden de materia.</summary>
    private static async Task<PlanCreado> CrearPlanAsync(HttpClient dgaa, int programaEducativoId, int cantidad)
    {
        var codigo = EscenarioOferta.CodigoDePlan();
        var planId = await EscenarioOferta.CrearPlanAsync(
            dgaa,
            programaEducativoId,
            codigo,
            Enumerable.Range(0, cantidad).Select(_ => EscenarioOferta.Experiencia()).ToList());

        using var respuesta = await dgaa.GetAsync(
            new Uri($"/api/v1/oferta-educativa/planes-estudio/{planId}/experiencias-educativas", UriKind.Relative), Cancelacion);
        var experiencias = (await EscenarioOferta.Leer(respuesta)).EnumerateArray()
            .Select(e => new ExperienciaCreada(
                e.GetProperty("id").GetInt32(),
                e.GetProperty("materia").GetString()!,
                e.GetProperty("nombre").GetString()!))
            .ToList();

        return new PlanCreado(planId, codigo, experiencias);
    }

    private Task<int> InsertarAsync(int periodoEscolarId, int experienciaEducativaId, string nrc, bool dadaDeBaja = false) =>
        DatosAcademicosSql.InsertarProgramacionDeExperienciaAsync(
            sqlServer.CadenaConexion, periodoEscolarId, experienciaEducativaId, dadaDeBaja, nrc);

    private Task<int> InsertarHorarioAsync(
        int programacionId,
        int sincronizacionId,
        byte dia,
        int horaInicio,
        int horaFin,
        DateOnly fechaInicio,
        DateOnly fechaFin,
        string? edificio = null,
        string? aula = null) =>
        DatosAcademicosSql.InsertarHorarioAsync(
            sqlServer.CadenaConexion,
            programacionId,
            sincronizacionId,
            dia,
            new TimeOnly(horaInicio, 0),
            new TimeOnly(horaFin, 0),
            fechaInicio,
            fechaFin,
            edificio,
            aula);

    /// <summary>Con los filtros dados, el cliente ve exactamente estas programaciones, en este orden.</summary>
    private static async Task VerificaFiltroAsync(HttpClient cliente, string consulta, params int[] idsEsperados)
    {
        using var respuesta = await cliente.GetAsync(Uri($"?{consulta}"), Cancelacion);
        var pagina = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        Ids(pagina).ShouldBe(idsEsperados, consulta);
        pagina.GetProperty("total").GetInt32().ShouldBe(idsEsperados.Length, consulta);
    }

    private static async Task VerificaNoEncontradoAsync(HttpResponseMessage respuesta)
    {
        var problema = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problema.GetProperty("codigo").GetString().ShouldBe("ProgramacionAcademica.NoEncontrado");
    }

    private sealed record ExperienciaCreada(int Id, string Materia, string Nombre);

    private sealed record PlanCreado(int Id, string Codigo, IReadOnlyList<ExperienciaCreada> Experiencias);
}
