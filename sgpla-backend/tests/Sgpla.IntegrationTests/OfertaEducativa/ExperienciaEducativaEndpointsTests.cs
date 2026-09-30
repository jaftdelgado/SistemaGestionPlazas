using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests.OfertaEducativa;

public sealed class ExperienciaEducativaEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private const string Ruta = "/api/v1/oferta-educativa/experiencias-educativas";
    private const string RutaPlanes = "/api/v1/oferta-educativa/planes-estudio";

    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Crear_ConDatosValidos_Responde201ConLocationYElRecursoNormalizado()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanAsync(escenario);

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(),
            Cuerpo(
                planId,
                ("nombre", "  Habilidades   de comunicación "),
                ("materia", " enso "),
                ("curso", "00001"),
                ("horasTeoricas", 2),
                ("horasPracticas", 3),
                ("creditos", 6),
                ("cupoMinimo", 5),
                ("cupoMaximo", 30),
                ("perfilDocente", "  Licenciatura en informática  "),
                ("areaFormacionId", 2)),
            Cancelacion);
        var creada = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = creada.GetProperty("id").GetInt32();
        creada.GetProperty("nombre").GetString().ShouldBe("Habilidades de comunicación");
        creada.GetProperty("materia").GetString().ShouldBe("ENSO");
        creada.GetProperty("curso").GetString().ShouldBe("00001");
        creada.GetProperty("horasTeoricas").GetInt32().ShouldBe(2);
        creada.GetProperty("horasPracticas").GetInt32().ShouldBe(3);
        creada.GetProperty("creditos").GetInt32().ShouldBe(6);
        creada.GetProperty("cupoMinimo").GetInt32().ShouldBe(5);
        creada.GetProperty("cupoMaximo").GetInt32().ShouldBe(30);
        creada.GetProperty("perfilDocente").GetString().ShouldBe("Licenciatura en informática");
        creada.GetProperty("areaFormacion").GetProperty("id").GetInt32().ShouldBe(2);
        creada.GetProperty("areaFormacion").GetProperty("clave").GetString().ShouldBe("112");
        creada.GetProperty("areaFormacion").GetProperty("nombre").GetString().ShouldBe("Área de Formación Disciplinaria");
        creada.GetProperty("planEstudios").GetProperty("id").GetInt32().ShouldBe(planId);
        creada.GetProperty("tuvoProgramaciones").GetBoolean().ShouldBeFalse();
        respuesta.Headers.Location.ShouldNotBeNull().AbsolutePath.ShouldBe($"{Ruta}/{id}");
        (await NumeroDeExperienciasDelPlanAsync(escenario.Dgaa, planId)).ShouldBe(2);
    }

    [Theory]
    [InlineData("nombre", "", "nombre", "ExperienciaEducativa.NombreVacio")]
    [InlineData("nombre", "   ", "nombre", "ExperienciaEducativa.NombreVacio")]
    [InlineData("materia", "", "materia", "ExperienciaEducativa.MateriaVacia")]
    [InlineData("materia", "EN-SO", "materia", "ExperienciaEducativa.MateriaFormatoInvalido")]
    [InlineData("curso", "  ", "curso", "ExperienciaEducativa.CursoVacio")]
    [InlineData("curso", "38 003", "curso", "ExperienciaEducativa.CursoFormatoInvalido")]
    [InlineData("horasTeoricas", -1, "horasTeoricas", "ExperienciaEducativa.HorasTeoricasNegativas")]
    [InlineData("horasPracticas", -1, "horasPracticas", "ExperienciaEducativa.HorasPracticasNegativas")]
    [InlineData("creditos", 0, "creditos", "ExperienciaEducativa.CreditosNoPositivos")]
    [InlineData("creditos", -2, "creditos", "ExperienciaEducativa.CreditosNoPositivos")]
    [InlineData("cupoMinimo", -1, "cupoMinimo", "ExperienciaEducativa.CupoMinimoNegativo")]
    [InlineData("cupoMaximo", -1, "cupoMaximo", "ExperienciaEducativa.CupoMaximoNegativo")]
    [InlineData("areaFormacionId", 999, "areaFormacionId", "ExperienciaEducativa.AreaFormacionInexistente")]
    public async Task Crear_ConDatoInvalido_Responde400ConErrorEnElCampo(
        string campoDelCuerpo, object valor, string campoDelError, string codigo)
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanAsync(escenario);

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(Uri(), Cuerpo(planId, (campoDelCuerpo, valor)), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, campoDelError, codigo);
    }

    [Fact]
    public async Task Crear_ConTextosDemasiadoLargos_Responde400ConErrorEnElCampo()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanAsync(escenario);

        using var nombre = await escenario.Dgaa.PostAsJsonAsync(Uri(), Cuerpo(planId, ("nombre", new string('A', 201))), Cancelacion);
        using var materia = await escenario.Dgaa.PostAsJsonAsync(Uri(), Cuerpo(planId, ("materia", new string('A', 51))), Cancelacion);
        using var curso = await escenario.Dgaa.PostAsJsonAsync(Uri(), Cuerpo(planId, ("curso", new string('1', 51))), Cancelacion);

        await VerificaErrorDeCampoAsync(nombre, "nombre", "ExperienciaEducativa.NombreDemasiadoLargo");
        await VerificaErrorDeCampoAsync(materia, "materia", "ExperienciaEducativa.MateriaDemasiadoLarga");
        await VerificaErrorDeCampoAsync(curso, "curso", "ExperienciaEducativa.CursoDemasiadoLargo");
    }

    [Fact]
    public async Task Crear_ConCuposInvertidos_Responde400ConErrorEnCupoMinimo()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanAsync(escenario);

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(planId, ("cupoMinimo", 30), ("cupoMaximo", 5)), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "cupoMinimo", "ExperienciaEducativa.CuposInvertidos");
    }

    [Fact]
    public async Task Crear_ConPlanInexistente_Responde400ConErrorEnPlanEstudiosId()
    {
        using var escenario = await NuevoEscenarioAsync();

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(Uri(), Cuerpo(int.MaxValue), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "planEstudiosId", "ExperienciaEducativa.PlanEstudiosInexistente");
    }

    [Fact]
    public async Task Crear_ConPlanDadoDeBaja_Responde400ConErrorEnPlanEstudiosId()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanAsync(escenario);
        (await escenario.Dgaa.DeleteAsync(RutaPlan(planId), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(Uri(), Cuerpo(planId), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "planEstudiosId", "ExperienciaEducativa.PlanEstudiosInexistente");
    }

    [Fact]
    public async Task Crear_ConPlanDeOtraArea_Responde400ConErrorEnPlanEstudiosId()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var propio = await EscenarioOferta.CrearAsync(_api, superusuario);
        using var ajeno = await EscenarioOferta.CrearAsync(_api, superusuario);
        var planAjeno = await CrearPlanAsync(ajeno);

        using var respuesta = await propio.Dgaa.PostAsJsonAsync(Uri(), Cuerpo(planAjeno), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "planEstudiosId", "ExperienciaEducativa.PlanEstudiosInexistente");
        (await NumeroDeExperienciasDelPlanAsync(ajeno.Dgaa, planAjeno)).ShouldBe(1);
    }

    [Fact]
    public async Task Crear_ConMateriaYCursoExistentesEnElPlan_Responde409()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanAsync(escenario);
        await CrearExperienciaAsync(escenario.Dgaa, planId, "ENSO", "38003");

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(planId, ("materia", " enso "), ("curso", "38003")), Cancelacion);
        var problema = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("ExperienciaEducativa.MateriaCursoDuplicado");
    }

    [Fact]
    public async Task Crear_ConMateriaYCursoDeUnaExperienciaDadaDeBaja_Responde409()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanAsync(escenario);
        var id = await CrearExperienciaAsync(escenario.Dgaa, planId, "ENSO", "38003");
        (await escenario.Dgaa.DeleteAsync(Uri($"/{id}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(planId, ("materia", "ENSO"), ("curso", "38003")), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Crear_ConLaMismaMateriaYCursoEnOtroPlan_Responde201()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanAsync(escenario);
        var otroPlanId = await CrearPlanAsync(escenario);
        await CrearExperienciaAsync(escenario.Dgaa, planId, "ENSO", "38003");

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(otroPlanId, ("materia", "ENSO"), ("curso", "38003")), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Obtener_Existente_Responde200ConTuvoProgramaciones()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var planId = await CrearPlanAsync(escenario);
        var sinProgramar = await CrearExperienciaAsync(escenario.Dgaa, planId, "ENSO", "38003");
        var programada = await CrearExperienciaAsync(escenario.Dgaa, planId, "FBGR", "80001");
        var periodoId = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        await DatosAcademicosSql.InsertarProgramacionDeExperienciaAsync(sqlServer.CadenaConexion, periodoId, programada, dadaDeBaja: true);

        using var uno = await escenario.Dgaa.GetAsync(Uri($"/{sinProgramar}"), Cancelacion);
        using var otra = await escenario.Dgaa.GetAsync(Uri($"/{programada}"), Cancelacion);
        var sinProgramarJson = await EscenarioOferta.Leer(uno);
        var programadaJson = await EscenarioOferta.Leer(otra);

        uno.StatusCode.ShouldBe(HttpStatusCode.OK);
        sinProgramarJson.GetProperty("id").GetInt32().ShouldBe(sinProgramar);
        sinProgramarJson.GetProperty("materia").GetString().ShouldBe("ENSO");
        sinProgramarJson.GetProperty("planEstudios").GetProperty("id").GetInt32().ShouldBe(planId);
        sinProgramarJson.GetProperty("tuvoProgramaciones").GetBoolean().ShouldBeFalse();
        otra.StatusCode.ShouldBe(HttpStatusCode.OK);
        programadaJson.GetProperty("tuvoProgramaciones").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Obtener_Inexistente_Responde404ConCodigo()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await superusuario.GetAsync(Uri($"/{int.MaxValue}"), Cancelacion);

        await VerificaNoEncontradoAsync(respuesta);
    }

    [Fact]
    public async Task Obtener_DadaDeBaja_Responde404()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanAsync(escenario);
        var id = await CrearExperienciaAsync(escenario.Dgaa, planId, "ENSO", "38003");
        (await escenario.Dgaa.DeleteAsync(Uri($"/{id}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await escenario.Dgaa.GetAsync(Uri($"/{id}"), Cancelacion);

        await VerificaNoEncontradoAsync(respuesta);
    }

    [Fact]
    public async Task Modificar_SinProgramaciones_Responde204YCambiaTodoLoEditable()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanAsync(escenario);
        var id = await CrearExperienciaAsync(escenario.Dgaa, planId, "ENSO", "38003");

        using var respuesta = await escenario.Dgaa.PutAsJsonAsync(
            Uri($"/{id}"),
            Modificacion(
                ("nombre", "  Otro   nombre "),
                ("horasTeoricas", 4),
                ("horasPracticas", 5),
                ("creditos", 9),
                ("cupoMinimo", 10),
                ("cupoMaximo", 40),
                ("perfilDocente", " Otro perfil "),
                ("areaFormacionId", 3)),
            Cancelacion);
        using var obtener = await escenario.Dgaa.GetAsync(Uri($"/{id}"), Cancelacion);
        var actual = await EscenarioOferta.Leer(obtener);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        actual.GetProperty("nombre").GetString().ShouldBe("Otro nombre");
        actual.GetProperty("materia").GetString().ShouldBe("ENSO");
        actual.GetProperty("curso").GetString().ShouldBe("38003");
        actual.GetProperty("horasTeoricas").GetInt32().ShouldBe(4);
        actual.GetProperty("horasPracticas").GetInt32().ShouldBe(5);
        actual.GetProperty("creditos").GetInt32().ShouldBe(9);
        actual.GetProperty("cupoMinimo").GetInt32().ShouldBe(10);
        actual.GetProperty("cupoMaximo").GetInt32().ShouldBe(40);
        actual.GetProperty("perfilDocente").GetString().ShouldBe("Otro perfil");
        actual.GetProperty("areaFormacion").GetProperty("id").GetInt32().ShouldBe(3);
    }

    [Fact]
    public async Task Modificar_SinCuposNiPerfil_LosQuita()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanAsync(escenario);
        var id = await CrearExperienciaAsync(
            escenario.Dgaa, planId, "ENSO", "38003", ("cupoMinimo", 5), ("cupoMaximo", 30), ("perfilDocente", "Perfil"));

        using var respuesta = await escenario.Dgaa.PutAsJsonAsync(Uri($"/{id}"), Modificacion(), Cancelacion);
        using var obtener = await escenario.Dgaa.GetAsync(Uri($"/{id}"), Cancelacion);
        var actual = await EscenarioOferta.Leer(obtener);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        actual.GetProperty("cupoMinimo").ValueKind.ShouldBe(JsonValueKind.Null);
        actual.GetProperty("cupoMaximo").ValueKind.ShouldBe(JsonValueKind.Null);
        actual.GetProperty("perfilDocente").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Modificar_ConProgramacionActivaODadaDeBaja_Responde409AlCambiarHorasCreditosOArea(bool dadaDeBaja)
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var id = await CrearExperienciaConProgramacionAsync(escenario, superusuario, dadaDeBaja);

        using var horasTeoricas = await escenario.Dgaa.PutAsJsonAsync(Uri($"/{id}"), Modificacion(("horasTeoricas", 4)), Cancelacion);
        using var horasPracticas = await escenario.Dgaa.PutAsJsonAsync(Uri($"/{id}"), Modificacion(("horasPracticas", 4)), Cancelacion);
        using var creditos = await escenario.Dgaa.PutAsJsonAsync(Uri($"/{id}"), Modificacion(("creditos", 7)), Cancelacion);
        using var area = await escenario.Dgaa.PutAsJsonAsync(Uri($"/{id}"), Modificacion(("areaFormacionId", 2)), Cancelacion);

        foreach (var respuesta in new[] { horasTeoricas, horasPracticas, creditos, area })
        {
            var problema = await EscenarioOferta.Leer(respuesta);

            respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            problema.GetProperty("codigo").GetString().ShouldBe("ExperienciaEducativa.AtributosCurricularesInmutables");
        }

        using var obtener = await escenario.Dgaa.GetAsync(Uri($"/{id}"), Cancelacion);
        var actual = await EscenarioOferta.Leer(obtener);
        actual.GetProperty("horasTeoricas").GetInt32().ShouldBe(2);
        actual.GetProperty("horasPracticas").GetInt32().ShouldBe(2);
        actual.GetProperty("creditos").GetInt32().ShouldBe(6);
        actual.GetProperty("areaFormacion").GetProperty("id").GetInt32().ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Modificar_ConProgramacionActivaODadaDeBaja_Responde204AlCambiarSoloNombrePerfilYCupos(bool dadaDeBaja)
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var id = await CrearExperienciaConProgramacionAsync(escenario, superusuario, dadaDeBaja);

        using var respuesta = await escenario.Dgaa.PutAsJsonAsync(
            Uri($"/{id}"),
            Modificacion(("nombre", "Otro nombre"), ("cupoMinimo", 10), ("cupoMaximo", 40), ("perfilDocente", "Otro perfil")),
            Cancelacion);
        using var obtener = await escenario.Dgaa.GetAsync(Uri($"/{id}"), Cancelacion);
        var actual = await EscenarioOferta.Leer(obtener);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        actual.GetProperty("nombre").GetString().ShouldBe("Otro nombre");
        actual.GetProperty("cupoMinimo").GetInt32().ShouldBe(10);
        actual.GetProperty("cupoMaximo").GetInt32().ShouldBe(40);
        actual.GetProperty("perfilDocente").GetString().ShouldBe("Otro perfil");
        actual.GetProperty("tuvoProgramaciones").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Modificar_ConCuposInvertidos_Responde400ConErrorEnCupoMinimo()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanAsync(escenario);
        var id = await CrearExperienciaAsync(escenario.Dgaa, planId, "ENSO", "38003");

        using var respuesta = await escenario.Dgaa.PutAsJsonAsync(
            Uri($"/{id}"), Modificacion(("cupoMinimo", 40), ("cupoMaximo", 10)), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "cupoMinimo", "ExperienciaEducativa.CuposInvertidos");
    }

    [Theory]
    [InlineData("nombre", "  ", "nombre", "ExperienciaEducativa.NombreVacio")]
    [InlineData("horasTeoricas", -1, "horasTeoricas", "ExperienciaEducativa.HorasTeoricasNegativas")]
    [InlineData("creditos", 0, "creditos", "ExperienciaEducativa.CreditosNoPositivos")]
    [InlineData("cupoMaximo", -1, "cupoMaximo", "ExperienciaEducativa.CupoMaximoNegativo")]
    [InlineData("areaFormacionId", 999, "areaFormacionId", "ExperienciaEducativa.AreaFormacionInexistente")]
    public async Task Modificar_ConDatoInvalido_Responde400ConErrorEnElCampo(
        string campoDelCuerpo, object valor, string campoDelError, string codigo)
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanAsync(escenario);
        var id = await CrearExperienciaAsync(escenario.Dgaa, planId, "ENSO", "38003");

        using var respuesta = await escenario.Dgaa.PutAsJsonAsync(Uri($"/{id}"), Modificacion((campoDelCuerpo, valor)), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, campoDelError, codigo);
    }

    [Fact]
    public async Task Modificar_ConNombreLargo_Responde400ConErrorEnNombre()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanAsync(escenario);
        var id = await CrearExperienciaAsync(escenario.Dgaa, planId, "ENSO", "38003");

        using var respuesta = await escenario.Dgaa.PutAsJsonAsync(
            Uri($"/{id}"), Modificacion(("nombre", new string('A', 201))), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "nombre", "ExperienciaEducativa.NombreDemasiadoLargo");
    }

    [Fact]
    public async Task Modificar_Inexistente_DadaDeBajaODeOtraArea_Responde404()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var propio = await EscenarioOferta.CrearAsync(_api, superusuario);
        using var ajeno = await EscenarioOferta.CrearAsync(_api, superusuario);
        var idAjeno = await CrearExperienciaAsync(ajeno.Dgaa, await CrearPlanAsync(ajeno), "ENSO", "38003");
        var idDadaDeBaja = await CrearExperienciaAsync(propio.Dgaa, await CrearPlanAsync(propio), "ENSO", "38003");
        (await propio.Dgaa.DeleteAsync(Uri($"/{idDadaDeBaja}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var inexistente = await propio.Dgaa.PutAsJsonAsync(Uri($"/{int.MaxValue}"), Modificacion(), Cancelacion);
        using var dadaDeBaja = await propio.Dgaa.PutAsJsonAsync(Uri($"/{idDadaDeBaja}"), Modificacion(), Cancelacion);
        using var deOtraArea = await propio.Dgaa.PutAsJsonAsync(Uri($"/{idAjeno}"), Modificacion(("nombre", "Intruso")), Cancelacion);

        await VerificaNoEncontradoAsync(inexistente);
        await VerificaNoEncontradoAsync(dadaDeBaja);
        await VerificaNoEncontradoAsync(deOtraArea);
        using var delAjeno = await ajeno.Dgaa.GetAsync(Uri($"/{idAjeno}"), Cancelacion);
        (await EscenarioOferta.Leer(delAjeno)).GetProperty("nombre").GetString().ShouldNotBe("Intruso");
    }

    [Fact]
    public async Task DarDeBaja_SinProgramaciones_Responde204YLaExperienciaDejaDeVerse()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanAsync(escenario);
        var id = await CrearExperienciaAsync(escenario.Dgaa, planId, "ENSO", "38003");

        using var respuesta = await escenario.Dgaa.DeleteAsync(Uri($"/{id}"), Cancelacion);
        using var obtener = await escenario.Dgaa.GetAsync(Uri($"/{id}"), Cancelacion);
        using var otraVez = await escenario.Dgaa.DeleteAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await VerificaNoEncontradoAsync(obtener);
        otraVez.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await NumeroDeExperienciasDelPlanAsync(escenario.Dgaa, planId)).ShouldBe(1);
    }

    [Fact]
    public async Task DarDeBaja_DeLaUltimaExperienciaActiva_NoDaDeBajaElPlan()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanAsync(escenario, materia: "ENSO", curso: "38003");
        var id = (await ExperienciasDelPlanAsync(escenario.Dgaa, planId)).Single();

        (await escenario.Dgaa.DeleteAsync(Uri($"/{id}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var plan = await escenario.Dgaa.GetAsync(new Uri($"{RutaPlanes}/{planId}", UriKind.Relative), Cancelacion);
        var planJson = await EscenarioOferta.Leer(plan);

        plan.StatusCode.ShouldBe(HttpStatusCode.OK);
        planJson.GetProperty("experienciasEducativas").GetInt32().ShouldBe(0);
        (await ExperienciasDelPlanAsync(escenario.Dgaa, planId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task DarDeBaja_ConProgramacionActiva_Responde409()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var id = await CrearExperienciaConProgramacionAsync(escenario, superusuario, dadaDeBaja: false);

        using var respuesta = await escenario.Dgaa.DeleteAsync(Uri($"/{id}"), Cancelacion);
        var problema = await EscenarioOferta.Leer(respuesta);
        using var obtener = await escenario.Dgaa.GetAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("ExperienciaEducativa.TieneProgramacionesActivas");
        obtener.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DarDeBaja_ConProgramacionesSoloDadasDeBaja_Responde204()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var id = await CrearExperienciaConProgramacionAsync(escenario, superusuario, dadaDeBaja: true);

        using var respuesta = await escenario.Dgaa.DeleteAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DarDeBaja_DeUnaExperienciaDeOtraArea_Responde404()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var propio = await EscenarioOferta.CrearAsync(_api, superusuario);
        using var ajeno = await EscenarioOferta.CrearAsync(_api, superusuario);
        var idAjeno = await CrearExperienciaAsync(ajeno.Dgaa, await CrearPlanAsync(ajeno), "ENSO", "38003");

        using var respuesta = await propio.Dgaa.DeleteAsync(Uri($"/{idAjeno}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ajeno.Dgaa.GetAsync(Uri($"/{idAjeno}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DarDeBajaElPlan_DejaDeVerseSusExperiencias()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanAsync(escenario);
        var id = await CrearExperienciaAsync(escenario.Dgaa, planId, "ENSO", "38003");

        (await escenario.Dgaa.DeleteAsync(RutaPlan(planId), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var respuesta = await escenario.Dgaa.GetAsync(Uri($"/{id}"), Cancelacion);
        using var modificar = await escenario.Dgaa.PutAsJsonAsync(Uri($"/{id}"), Modificacion(), Cancelacion);
        using var darDeBaja = await escenario.Dgaa.DeleteAsync(Uri($"/{id}"), Cancelacion);

        await VerificaNoEncontradoAsync(respuesta);
        modificar.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        darDeBaja.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Escribir_ComoSuperusuario_Responde403()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var planId = await CrearPlanAsync(escenario);
        var id = await CrearExperienciaAsync(escenario.Dgaa, planId, "ENSO", "38003");

        await VerificaEscrituraProhibidaAsync(superusuario, planId, id);
    }

    [Fact]
    public async Task Escribir_ComoEntidadAcademica_Responde403()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var planId = await CrearPlanAsync(escenario);
        var id = await CrearExperienciaAsync(escenario.Dgaa, planId, "ENSO", "38003");
        using var entidadAcademica = await _api.CrearClienteEntidadAcademicaAsync(escenario.EntidadId);

        await VerificaEscrituraProhibidaAsync(entidadAcademica, planId, id);
    }

    [Fact]
    public async Task Ambito_ElSuperusuarioYLaEntidadAcademicaLeenLoSuyo_YUnDgaaDeOtraAreaRecibe404()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var propio = await EscenarioOferta.CrearAsync(_api, superusuario);
        using var ajeno = await EscenarioOferta.CrearAsync(_api, superusuario);
        var idPropio = await CrearExperienciaAsync(propio.Dgaa, await CrearPlanAsync(propio), "ENSO", "38003");
        var idAjeno = await CrearExperienciaAsync(ajeno.Dgaa, await CrearPlanAsync(ajeno), "ENSO", "38003");
        using var entidadAcademica = await _api.CrearClienteEntidadAcademicaAsync(propio.EntidadId);

        (await superusuario.GetAsync(Uri($"/{idPropio}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await superusuario.GetAsync(Uri($"/{idAjeno}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await entidadAcademica.GetAsync(Uri($"/{idPropio}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.OK);
        using var deOtraEntidad = await entidadAcademica.GetAsync(Uri($"/{idAjeno}"), Cancelacion);
        using var deOtraArea = await propio.Dgaa.GetAsync(Uri($"/{idAjeno}"), Cancelacion);

        await VerificaNoEncontradoAsync(deOtraEntidad);
        await VerificaNoEncontradoAsync(deOtraArea);
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static Uri Uri(string sufijo = "") => new($"{Ruta}{sufijo}", UriKind.Relative);

    private static Uri RutaPlan(int id) => new($"{RutaPlanes}/{id}", UriKind.Relative);

    /// <summary>Cuerpo de un alta válido; <paramref name="cambios"/> reemplaza campos por nombre.</summary>
    private static Dictionary<string, object?> Cuerpo(int planEstudiosId, params (string Campo, object? Valor)[] cambios)
    {
        var cuerpo = new Dictionary<string, object?>
        {
            ["planEstudiosId"] = planEstudiosId,
            ["nombre"] = DatosUnicos.Nombre("Experiencia"),
            ["materia"] = DatosUnicos.ClaveAlfanumerica(),
            ["curso"] = "00001",
            ["horasTeoricas"] = 2,
            ["horasPracticas"] = 2,
            ["creditos"] = 6,
            ["cupoMinimo"] = null,
            ["cupoMaximo"] = null,
            ["perfilDocente"] = null,
            ["areaFormacionId"] = 1,
        };

        foreach (var (campo, valor) in cambios)
        {
            cuerpo[campo] = valor;
        }

        return cuerpo;
    }

    /// <summary>Cuerpo de una modificación con los mismos valores que deja <see cref="Cuerpo"/>; <paramref name="cambios"/> los reemplaza.</summary>
    private static Dictionary<string, object?> Modificacion(params (string Campo, object? Valor)[] cambios)
    {
        var cuerpo = Cuerpo(0, cambios);
        cuerpo.Remove("planEstudiosId");
        cuerpo.Remove("materia");
        cuerpo.Remove("curso");

        return cuerpo;
    }

    private static async Task VerificaErrorDeCampoAsync(HttpResponseMessage respuesta, string campo, string codigo)
    {
        var problema = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty(campo, out _).ShouldBeTrue();
        problema.GetProperty("codigo").GetString().ShouldBe(codigo);
    }

    private static async Task VerificaNoEncontradoAsync(HttpResponseMessage respuesta)
    {
        var problema = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problema.GetProperty("codigo").GetString().ShouldBe("ExperienciaEducativa.NoEncontrado");
    }

    private static async Task VerificaEscrituraProhibidaAsync(HttpClient cliente, int planId, int experienciaId)
    {
        using var crear = await cliente.PostAsJsonAsync(Uri(), Cuerpo(planId), Cancelacion);
        using var modificar = await cliente.PutAsJsonAsync(Uri($"/{experienciaId}"), Modificacion(), Cancelacion);
        using var darDeBaja = await cliente.DeleteAsync(Uri($"/{experienciaId}"), Cancelacion);

        crear.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        modificar.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        darDeBaja.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<EscenarioOferta> NuevoEscenarioAsync()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        return await EscenarioOferta.CrearAsync(_api, superusuario);
    }

    /// <summary>Un plan con una sola EE (materia única, salvo que se indique), para que la prueba agregue las suyas.</summary>
    private static Task<int> CrearPlanAsync(EscenarioOferta escenario, string? materia = null, string curso = "99999") =>
        EscenarioOferta.CrearPlanAsync(
            escenario.Dgaa,
            escenario.ProgramaId,
            EscenarioOferta.CodigoDePlan(),
            [EscenarioOferta.Experiencia(materia: materia, curso: curso)]);

    private static async Task<int> CrearExperienciaAsync(
        HttpClient dgaa, int planId, string materia, string curso, params (string Campo, object? Valor)[] cambios)
    {
        using var respuesta = await dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(planId, [("materia", materia), ("curso", curso), .. cambios]), Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await EscenarioOferta.Leer(respuesta)).GetProperty("id").GetInt32();
    }

    /// <summary>Crea un plan con una EE y le inserta una programación, activa o dada de baja.</summary>
    private async Task<int> CrearExperienciaConProgramacionAsync(EscenarioOferta escenario, HttpClient superusuario, bool dadaDeBaja)
    {
        var planId = await CrearPlanAsync(escenario);
        var id = await CrearExperienciaAsync(escenario.Dgaa, planId, "ENSO", "38003");
        var periodoId = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        await DatosAcademicosSql.InsertarProgramacionDeExperienciaAsync(sqlServer.CadenaConexion, periodoId, id, dadaDeBaja);

        return id;
    }

    private static async Task<List<int>> ExperienciasDelPlanAsync(HttpClient cliente, int planId)
    {
        using var respuesta = await cliente.GetAsync(
            new Uri($"{RutaPlanes}/{planId}/experiencias-educativas", UriKind.Relative), Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);

        return (await EscenarioOferta.Leer(respuesta)).EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToList();
    }

    private static async Task<int> NumeroDeExperienciasDelPlanAsync(HttpClient cliente, int planId) =>
        (await ExperienciasDelPlanAsync(cliente, planId)).Count;
}
