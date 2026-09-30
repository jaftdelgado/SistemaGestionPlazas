using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests.OfertaEducativa;

public sealed partial class PlanEstudiosEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private const string Ruta = "/api/v1/oferta-educativa/planes-estudio";

    /// <summary>Los 14 encabezados del formato de la UV, en orden (Modulo_OfertaEducativa.md, D7).</summary>
    private static readonly string[] EncabezadosDeLaUv =
    [
        "DESC_AREA_ACAD", "CODIGO_PLAN", "DESCRIPCION", "CODIGO_PER_CAT", "DESC_PER_CAT", "MATERIA_EE", "CURSO_EE", "DESC_EE",
        "HT_EE", "HP_EE", "CREDITOS_EE", "CODE_AREA_F", "DESC_AREA_F", "PERFIL_DOC",
    ];

    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Importar_ConElPlanDeEjemploDeLaUv_Responde201Con58ExperienciasYLaRespuestaAnidada()
    {
        using var escenario = await NuevoEscenarioAsync();

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(),
            new
            {
                programaEducativoId = escenario.ProgramaId,
                codigo = PlanEjemploIsof14.Codigo,
                experienciasEducativas = PlanEjemploIsof14.ExperienciasEducativas,
            },
            Cancelacion);
        var creado = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = creado.GetProperty("id").GetInt32();
        creado.GetProperty("codigo").GetString().ShouldBe("ISOF-14-E-CR");
        creado.GetProperty("programaEducativo").GetProperty("id").GetInt32().ShouldBe(escenario.ProgramaId);
        creado.GetProperty("programaEducativo").GetProperty("nombre").GetString().ShouldBe(escenario.ProgramaNombre);
        creado.GetProperty("entidadAcademica").GetProperty("id").GetInt32().ShouldBe(escenario.EntidadId);
        creado.GetProperty("entidadAcademica").GetProperty("clave").GetString().ShouldBe(escenario.EntidadClave);
        creado.GetProperty("entidadAcademica").GetProperty("nombre").GetString().ShouldBe(escenario.EntidadNombre);
        creado.GetProperty("experienciasEducativas").GetInt32().ShouldBe(58);
        respuesta.Headers.Location.ShouldNotBeNull().AbsolutePath.ShouldBe($"{Ruta}/{id}");
    }

    [Fact]
    public async Task Importar_ConElPlanDeEjemploDeLaUv_GuardaCadaExperienciaTalComoSeNormaliza()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await EscenarioOferta.CrearPlanAsync(
            escenario.Dgaa, escenario.ProgramaId, PlanEjemploIsof14.Codigo, PlanEjemploIsof14.ExperienciasEducativas);

        using var respuesta = await escenario.Dgaa.GetAsync(Uri($"/{planId}/experiencias-educativas"), Cancelacion);
        var experiencias = (await EscenarioOferta.Leer(respuesta)).EnumerateArray().ToList();

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        experiencias.Count.ShouldBe(58);
        experiencias.GroupBy(e => e.GetProperty("areaFormacion").GetProperty("clave").GetString())
            .ToDictionary(g => g.Key!, g => g.Count())
            .ShouldBe(new Dictionary<string, int> { ["111"] = 18, ["112"] = 22, ["113"] = 18 }, ignoreOrder: true);

        foreach (var esperada in PlanEjemploIsof14.ExperienciasEducativas)
        {
            var guardada = experiencias.Single(e =>
                e.GetProperty("materia").GetString() == esperada.Materia && e.GetProperty("curso").GetString() == esperada.Curso);

            guardada.GetProperty("nombre").GetString().ShouldBe(EspaciosRepetidos().Replace(esperada.Nombre.Trim(), " "));
            guardada.GetProperty("horasTeoricas").GetInt32().ShouldBe(esperada.HorasTeoricas);
            guardada.GetProperty("horasPracticas").GetInt32().ShouldBe(esperada.HorasPracticas);
            guardada.GetProperty("creditos").GetInt32().ShouldBe(esperada.Creditos);
            guardada.GetProperty("areaFormacion").GetProperty("id").GetInt32().ShouldBe(esperada.AreaFormacionId);
            guardada.GetProperty("perfilDocente").GetString().ShouldBe(esperada.PerfilDocente?.Trim());
            guardada.GetProperty("cupoMinimo").ValueKind.ShouldBe(JsonValueKind.Null);
            guardada.GetProperty("cupoMaximo").ValueKind.ShouldBe(JsonValueKind.Null);
        }

        var exav = experiencias.Single(e => e.GetProperty("materia").GetString() == "EXAV");
        exav.GetProperty("curso").GetString().ShouldBe("00001");
        experiencias.Count(e => e.GetProperty("perfilDocente").ValueKind == JsonValueKind.Null).ShouldBe(6);
    }

    [Fact]
    public async Task Importar_ConCodigoEnMinusculasYEspacios_LoGuardaEnMayusculas()
    {
        using var escenario = await NuevoEscenarioAsync();

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(escenario.ProgramaId, "  isof-14-e-cr ", [EscenarioOferta.Experiencia()]), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await EscenarioOferta.Leer(respuesta)).GetProperty("codigo").GetString().ShouldBe("ISOF-14-E-CR");
    }

    [Fact]
    public async Task Importar_ConCreditosCeroEnLaCuartaExperiencia_Responde400ConElIndiceYNoCreaNada()
    {
        using var escenario = await NuevoEscenarioAsync();
        var experiencias = PlanEjemploIsof14.ExperienciasEducativas.ToArray();
        experiencias[3] = experiencias[3] with { Creditos = 0 };

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(escenario.ProgramaId, PlanEjemploIsof14.Codigo, experiencias), Cancelacion);

        await VerificaErrorDeCampoAsync(
            respuesta, "experienciasEducativas[3].creditos", "ExperienciaEducativa.CreditosNoPositivos");
        await VerificaQueNoHayPlanesAsync(escenario);
    }

    [Fact]
    public async Task Importar_ConVariasExperienciasInvalidas_RespondeSoloElPrimerError()
    {
        using var escenario = await NuevoEscenarioAsync();
        var experiencias = new[]
        {
            EscenarioOferta.Experiencia(),
            EscenarioOferta.Experiencia(nombre: "   "),
            EscenarioOferta.Experiencia(creditos: 0),
        };

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(escenario.ProgramaId, EscenarioOferta.CodigoDePlan(), experiencias), Cancelacion);
        var problema = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").EnumerateObject().Select(p => p.Name).ShouldBe(["experienciasEducativas[1].nombre"]);
    }

    [Fact]
    public async Task Importar_ConAreaInexistenteEnLaSeptimaExperiencia_Responde400ConElIndiceYNoCreaNada()
    {
        using var escenario = await NuevoEscenarioAsync();
        var experiencias = PlanEjemploIsof14.ExperienciasEducativas.ToArray();
        experiencias[6] = experiencias[6] with { AreaFormacionId = 999 };

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(escenario.ProgramaId, PlanEjemploIsof14.Codigo, experiencias), Cancelacion);

        await VerificaErrorDeCampoAsync(
            respuesta, "experienciasEducativas[6].areaFormacionId", "PlanEstudios.AreaFormacionInexistente");
        await VerificaQueNoHayPlanesAsync(escenario);
    }

    [Fact]
    public async Task Importar_ConExperienciaRepetida_Responde400ConElIndiceDeLaSegundaYNoCreaNada()
    {
        using var escenario = await NuevoEscenarioAsync();
        var experiencias = new[]
        {
            EscenarioOferta.Experiencia(materia: "FBGR", curso: "80001"),
            EscenarioOferta.Experiencia(materia: "ENSO", curso: "38003"),
            EscenarioOferta.Experiencia(materia: " enso ", curso: "38003"),
        };

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(escenario.ProgramaId, EscenarioOferta.CodigoDePlan(), experiencias), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "experienciasEducativas[2].curso", "PlanEstudios.ExperienciaRepetida");
        await VerificaQueNoHayPlanesAsync(escenario);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Importar_SinCodigo_Responde400ConErrorEnCodigo(string codigo)
    {
        using var escenario = await NuevoEscenarioAsync();

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(escenario.ProgramaId, codigo, [EscenarioOferta.Experiencia()]), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "codigo", "PlanEstudios.CodigoVacio");
    }

    [Fact]
    public async Task Importar_ConCodigoDemasiadoLargo_Responde400ConErrorEnCodigo()
    {
        using var escenario = await NuevoEscenarioAsync();

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(escenario.ProgramaId, new string('A', 51), [EscenarioOferta.Experiencia()]), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "codigo", "PlanEstudios.CodigoDemasiadoLargo");
    }

    [Fact]
    public async Task Importar_ConCodigoDeFormatoInvalido_Responde400ConErrorEnCodigo()
    {
        using var escenario = await NuevoEscenarioAsync();

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(escenario.ProgramaId, "ISOF_14", [EscenarioOferta.Experiencia()]), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "codigo", "PlanEstudios.CodigoFormatoInvalido");
    }

    [Fact]
    public async Task Importar_ConListaVacia_Responde400ConSinExperiencias()
    {
        using var escenario = await NuevoEscenarioAsync();

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(escenario.ProgramaId, EscenarioOferta.CodigoDePlan(), []), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "experienciasEducativas", "PlanEstudios.SinExperiencias");
    }

    [Fact]
    public async Task Importar_ConExperienciasEducativasNullOOmitidas_Responde400ConSinExperiencias()
    {
        using var escenario = await NuevoEscenarioAsync();
        var codigo = EscenarioOferta.CodigoDePlan();

        using var conNull = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), new { programaEducativoId = escenario.ProgramaId, codigo, experienciasEducativas = (object?)null }, Cancelacion);
        using var omitidas = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), new { programaEducativoId = escenario.ProgramaId, codigo }, Cancelacion);

        await VerificaErrorDeCampoAsync(conNull, "experienciasEducativas", "PlanEstudios.SinExperiencias");
        await VerificaErrorDeCampoAsync(omitidas, "experienciasEducativas", "PlanEstudios.SinExperiencias");
    }

    [Fact]
    public async Task Importar_ConTrescientasUnaExperiencias_Responde400ConDemasiadasExperiencias()
    {
        using var escenario = await NuevoEscenarioAsync();
        var experiencias = Enumerable.Range(0, 301).Select(i => EscenarioOferta.Experiencia(curso: $"{i:D5}")).ToList();

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(escenario.ProgramaId, EscenarioOferta.CodigoDePlan(), experiencias), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "experienciasEducativas", "PlanEstudios.DemasiadasExperiencias");
    }

    [Fact]
    public async Task Importar_ConProgramaInexistente_Responde400ConErrorEnProgramaEducativoId()
    {
        using var escenario = await NuevoEscenarioAsync();

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(int.MaxValue, EscenarioOferta.CodigoDePlan(), [EscenarioOferta.Experiencia()]), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "programaEducativoId", "PlanEstudios.ProgramaEducativoInexistente");
    }

    [Fact]
    public async Task Importar_ConProgramaDeOtraArea_Responde400ConErrorEnProgramaEducativoId()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var propio = await EscenarioOferta.CrearAsync(_api, superusuario);
        using var ajeno = await EscenarioOferta.CrearAsync(_api, superusuario);

        using var respuesta = await propio.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(ajeno.ProgramaId, EscenarioOferta.CodigoDePlan(), [EscenarioOferta.Experiencia()]), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "programaEducativoId", "PlanEstudios.ProgramaEducativoInexistente");
        await VerificaQueNoHayPlanesAsync(ajeno);
    }

    [Fact]
    public async Task Importar_ConProgramaDadoDeBaja_Responde400ConErrorEnProgramaEducativoId()
    {
        using var escenario = await NuevoEscenarioAsync();
        (await escenario.Dgaa.DeleteAsync(RutaPrograma(escenario.ProgramaId), Cancelacion))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(escenario.ProgramaId, EscenarioOferta.CodigoDePlan(), [EscenarioOferta.Experiencia()]), Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "programaEducativoId", "PlanEstudios.ProgramaEducativoInexistente");
    }

    [Fact]
    public async Task Importar_ConCodigoRepetidoEnElPrograma_Responde409()
    {
        using var escenario = await NuevoEscenarioAsync();
        var codigo = EscenarioOferta.CodigoDePlan();
        await EscenarioOferta.CrearPlanAsync(escenario.Dgaa, escenario.ProgramaId, codigo, [EscenarioOferta.Experiencia()]);

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(escenario.ProgramaId, codigo.ToLowerInvariant(), [EscenarioOferta.Experiencia()]), Cancelacion);
        var problema = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("PlanEstudios.CodigoDuplicado");
    }

    [Fact]
    public async Task Importar_ConElCodigoDeUnPlanDadoDeBaja_Responde409()
    {
        using var escenario = await NuevoEscenarioAsync();
        var codigo = EscenarioOferta.CodigoDePlan();
        var planId = await EscenarioOferta.CrearPlanAsync(
            escenario.Dgaa, escenario.ProgramaId, codigo, [EscenarioOferta.Experiencia()]);
        (await escenario.Dgaa.DeleteAsync(Uri($"/{planId}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(escenario.ProgramaId, codigo, [EscenarioOferta.Experiencia()]), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Importar_ConElMismoCodigoEnOtroPrograma_Responde201()
    {
        using var escenario = await NuevoEscenarioAsync();
        var codigo = EscenarioOferta.CodigoDePlan();
        await EscenarioOferta.CrearPlanAsync(escenario.Dgaa, escenario.ProgramaId, codigo, [EscenarioOferta.Experiencia()]);
        var (otroProgramaId, _) = await EscenarioOferta.CrearProgramaAsync(escenario.Dgaa, escenario.EntidadId);

        using var respuesta = await escenario.Dgaa.PostAsJsonAsync(
            Uri(), Cuerpo(otroProgramaId, codigo, [EscenarioOferta.Experiencia()]), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ListarExperienciasDelPlan_Responde200EnOrdenDeMateriaYCursoConTuvoProgramaciones()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var planId = await EscenarioOferta.CrearPlanAsync(
            escenario.Dgaa,
            escenario.ProgramaId,
            EscenarioOferta.CodigoDePlan(),
            [
                EscenarioOferta.Experiencia(materia: "FBGR", curso: "80002"),
                EscenarioOferta.Experiencia(materia: "ENSO", curso: "38003", nombre: "Habilidades", cupoMinimo: 5, cupoMaximo: 30, perfilDocente: "Perfil"),
                EscenarioOferta.Experiencia(materia: "FBGR", curso: "80001"),
            ]);
        var periodoId = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        var experiencias = await ExperienciasDelPlanAsync(escenario.Dgaa, planId);
        var conProgramacionActiva = experiencias.Single(e => e.Materia == "FBGR" && e.Curso == "80001").Id;
        var conProgramacionDadaDeBaja = experiencias.Single(e => e.Materia == "FBGR" && e.Curso == "80002").Id;
        await DatosAcademicosSql.InsertarProgramacionDeExperienciaAsync(sqlServer.CadenaConexion, periodoId, conProgramacionActiva);
        await DatosAcademicosSql.InsertarProgramacionDeExperienciaAsync(
            sqlServer.CadenaConexion, periodoId, conProgramacionDadaDeBaja, dadaDeBaja: true);

        using var respuesta = await escenario.Dgaa.GetAsync(Uri($"/{planId}/experiencias-educativas"), Cancelacion);
        var lista = (await EscenarioOferta.Leer(respuesta)).EnumerateArray().ToList();

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        lista.Select(e => (e.GetProperty("materia").GetString(), e.GetProperty("curso").GetString()))
            .ShouldBe([("ENSO", "38003"), ("FBGR", "80001"), ("FBGR", "80002")]);
        lista.Select(e => e.GetProperty("tuvoProgramaciones").GetBoolean()).ShouldBe([false, true, true]);

        var enso = lista[0];
        enso.GetProperty("nombre").GetString().ShouldBe("Habilidades");
        enso.GetProperty("horasTeoricas").GetInt32().ShouldBe(2);
        enso.GetProperty("horasPracticas").GetInt32().ShouldBe(2);
        enso.GetProperty("creditos").GetInt32().ShouldBe(6);
        enso.GetProperty("cupoMinimo").GetInt32().ShouldBe(5);
        enso.GetProperty("cupoMaximo").GetInt32().ShouldBe(30);
        enso.GetProperty("perfilDocente").GetString().ShouldBe("Perfil");
        enso.GetProperty("areaFormacion").GetProperty("id").GetInt32().ShouldBe(1);
        enso.GetProperty("areaFormacion").GetProperty("clave").GetString().ShouldBe("111");
        enso.GetProperty("areaFormacion").GetProperty("nombre").GetString().ShouldBe("Área de Formación Básica");
        enso.GetProperty("planEstudios").GetProperty("id").GetInt32().ShouldBe(planId);
    }

    [Fact]
    public async Task ListarExperienciasDelPlan_ConPlanInexistente_Responde404ConCodigo()
    {
        using var escenario = await NuevoEscenarioAsync();

        using var respuesta = await escenario.Dgaa.GetAsync(Uri($"/{int.MaxValue}/experiencias-educativas"), Cancelacion);

        await VerificaNoEncontradoAsync(respuesta);
    }

    [Fact]
    public async Task Obtener_Existente_Responde200ConLaRespuestaAnidada()
    {
        using var escenario = await NuevoEscenarioAsync();
        var codigo = EscenarioOferta.CodigoDePlan();
        var planId = await EscenarioOferta.CrearPlanAsync(
            escenario.Dgaa, escenario.ProgramaId, codigo, [EscenarioOferta.Experiencia(), EscenarioOferta.Experiencia()]);

        using var respuesta = await escenario.Dgaa.GetAsync(Uri($"/{planId}"), Cancelacion);
        var plan = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        plan.GetProperty("id").GetInt32().ShouldBe(planId);
        plan.GetProperty("codigo").GetString().ShouldBe(codigo);
        plan.GetProperty("programaEducativo").GetProperty("id").GetInt32().ShouldBe(escenario.ProgramaId);
        plan.GetProperty("entidadAcademica").GetProperty("id").GetInt32().ShouldBe(escenario.EntidadId);
        plan.GetProperty("experienciasEducativas").GetInt32().ShouldBe(2);
    }

    [Fact]
    public async Task Obtener_Inexistente_Responde404ConCodigo()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await superusuario.GetAsync(Uri($"/{int.MaxValue}"), Cancelacion);

        await VerificaNoEncontradoAsync(respuesta);
    }

    [Fact]
    public async Task Obtener_DadoDeBaja_Responde404()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanSimpleAsync(escenario);
        (await escenario.Dgaa.DeleteAsync(Uri($"/{planId}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await escenario.Dgaa.GetAsync(Uri($"/{planId}"), Cancelacion);

        await VerificaNoEncontradoAsync(respuesta);
    }

    [Fact]
    public async Task Listar_ConVariosPlanes_LosDevuelveEnOrdenDeCodigoYLosPagina()
    {
        using var escenario = await NuevoEscenarioAsync();
        var zeta = await EscenarioOferta.CrearPlanAsync(escenario.Dgaa, escenario.ProgramaId, "ZZ-1", [EscenarioOferta.Experiencia()]);
        var alfa = await EscenarioOferta.CrearPlanAsync(escenario.Dgaa, escenario.ProgramaId, "AA-1", [EscenarioOferta.Experiencia()]);
        var medio = await EscenarioOferta.CrearPlanAsync(escenario.Dgaa, escenario.ProgramaId, "MM-1", [EscenarioOferta.Experiencia()]);

        using var todos = await escenario.Dgaa.GetAsync(Uri($"?programaEducativoId={escenario.ProgramaId}"), Cancelacion);
        using var segunda = await escenario.Dgaa.GetAsync(
            Uri($"?programaEducativoId={escenario.ProgramaId}&pagina=2&tamanoPagina=2"), Cancelacion);
        var paginaTodos = await EscenarioOferta.Leer(todos);
        var paginaSegunda = await EscenarioOferta.Leer(segunda);

        todos.StatusCode.ShouldBe(HttpStatusCode.OK);
        Ids(paginaTodos).ShouldBe([alfa, medio, zeta]);
        paginaTodos.GetProperty("total").GetInt32().ShouldBe(3);
        paginaTodos.GetProperty("pagina").GetInt32().ShouldBe(1);
        paginaTodos.GetProperty("tamanoPagina").GetInt32().ShouldBe(20);
        Ids(paginaSegunda).ShouldBe([zeta]);
        paginaSegunda.GetProperty("total").GetInt32().ShouldBe(3);
        paginaSegunda.GetProperty("pagina").GetInt32().ShouldBe(2);
    }

    [Fact]
    public async Task Listar_ConFiltros_DevuelveSoloLosQueCoinciden()
    {
        using var escenario = await NuevoEscenarioAsync();
        var (otroProgramaId, _) = await EscenarioOferta.CrearProgramaAsync(escenario.Dgaa, escenario.EntidadId);
        var isof = await EscenarioOferta.CrearPlanAsync(
            escenario.Dgaa, escenario.ProgramaId, "ISOF-14-E-CR", [EscenarioOferta.Experiencia()]);
        var lsca = await EscenarioOferta.CrearPlanAsync(
            escenario.Dgaa, otroProgramaId, "LSCA-2020", [EscenarioOferta.Experiencia()]);
        var entidad = $"entidadAcademicaId={escenario.EntidadId}";

        await VerificaFiltroAsync(escenario.Dgaa, entidad, isof, lsca);
        await VerificaFiltroAsync(escenario.Dgaa, $"programaEducativoId={escenario.ProgramaId}", isof);
        await VerificaFiltroAsync(escenario.Dgaa, $"programaEducativoId={otroProgramaId}", lsca);
        await VerificaFiltroAsync(escenario.Dgaa, $"{entidad}&busqueda=isof", isof);
        await VerificaFiltroAsync(escenario.Dgaa, $"{entidad}&busqueda={System.Uri.EscapeDataString("  isof-14  ")}", isof);
        await VerificaFiltroAsync(escenario.Dgaa, $"{entidad}&busqueda=2020", lsca);
        await VerificaFiltroAsync(escenario.Dgaa, $"{entidad}&busqueda=%20%20", isof, lsca);
        await VerificaFiltroAsync(escenario.Dgaa, $"{entidad}&busqueda=zzz");
        await VerificaFiltroAsync(escenario.Dgaa, $"programaEducativoId={otroProgramaId}&busqueda=isof");
    }

    [Theory]
    [InlineData("pagina=0", "pagina")]
    [InlineData("tamanoPagina=101", "tamanoPagina")]
    [InlineData("programaEducativoId=0", "programaEducativoId")]
    [InlineData("entidadAcademicaId=-1", "entidadAcademicaId")]
    public async Task Listar_ConParametrosInvalidos_Responde400ConErrorEnElParametro(string consulta, string campo)
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await superusuario.GetAsync(Uri($"?{consulta}"), Cancelacion);
        var problema = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty(campo, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Listar_ConBusquedaDemasiadoLarga_Responde400()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await superusuario.GetAsync(Uri($"?busqueda={new string('A', 201)}"), Cancelacion);
        var problema = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty("busqueda", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task DarDeBaja_ConPlanSinProgramaciones_Responde204YElPlanYSusExperienciasDejanDeVerse()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await EscenarioOferta.CrearPlanAsync(
            escenario.Dgaa, escenario.ProgramaId, EscenarioOferta.CodigoDePlan(),
            [EscenarioOferta.Experiencia(), EscenarioOferta.Experiencia()]);

        using var respuesta = await escenario.Dgaa.DeleteAsync(Uri($"/{planId}"), Cancelacion);
        using var obtener = await escenario.Dgaa.GetAsync(Uri($"/{planId}"), Cancelacion);
        using var experiencias = await escenario.Dgaa.GetAsync(Uri($"/{planId}/experiencias-educativas"), Cancelacion);
        using var otraVez = await escenario.Dgaa.DeleteAsync(Uri($"/{planId}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await VerificaNoEncontradoAsync(obtener);
        await VerificaNoEncontradoAsync(experiencias);
        otraVez.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DarDeBaja_ConUnaExperienciaConProgramacionActiva_Responde409YNoDaDeBajaNada()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var planId = await CrearPlanSimpleAsync(escenario);
        var periodoId = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        var experienciaId = (await ExperienciasDelPlanAsync(escenario.Dgaa, planId)).Single().Id;
        await DatosAcademicosSql.InsertarProgramacionDeExperienciaAsync(sqlServer.CadenaConexion, periodoId, experienciaId);

        using var respuesta = await escenario.Dgaa.DeleteAsync(Uri($"/{planId}"), Cancelacion);
        var problema = await EscenarioOferta.Leer(respuesta);
        using var obtener = await escenario.Dgaa.GetAsync(Uri($"/{planId}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("PlanEstudios.ExperienciasConProgramacionesActivas");
        obtener.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ExperienciasDelPlanAsync(escenario.Dgaa, planId)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task DarDeBaja_ConProgramacionesSoloDadasDeBaja_Responde204()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var planId = await CrearPlanSimpleAsync(escenario);
        var periodoId = await EscenarioOferta.CrearPeriodoAsync(superusuario);
        var experienciaId = (await ExperienciasDelPlanAsync(escenario.Dgaa, planId)).Single().Id;
        await DatosAcademicosSql.InsertarProgramacionDeExperienciaAsync(
            sqlServer.CadenaConexion, periodoId, experienciaId, dadaDeBaja: true);

        using var respuesta = await escenario.Dgaa.DeleteAsync(Uri($"/{planId}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DarDeBaja_DeUnProgramaConPlanActivo_Responde409YConElPlanDadoDeBajaResponde204()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanSimpleAsync(escenario);

        using var conPlanActivo = await escenario.Dgaa.DeleteAsync(RutaPrograma(escenario.ProgramaId), Cancelacion);
        var problema = await EscenarioOferta.Leer(conPlanActivo);
        (await escenario.Dgaa.DeleteAsync(Uri($"/{planId}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var sinPlanActivo = await escenario.Dgaa.DeleteAsync(RutaPrograma(escenario.ProgramaId), Cancelacion);

        conPlanActivo.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("ProgramaEducativo.TienePlanesActivos");
        sinPlanActivo.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Modificar_ClasificacionDeUnProgramaConPlanCreadoPorLaApi_Responde409()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanSimpleAsync(escenario);
        (await escenario.Dgaa.DeleteAsync(Uri($"/{planId}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // El plan ya está dado de baja, pero el programa lo tuvo: sistema y nivel quedan inmutables.
        using var respuesta = await escenario.Dgaa.PutAsJsonAsync(
            RutaPrograma(escenario.ProgramaId),
            new { nombre = escenario.ProgramaNombre, sistemaEducativoId = 2, nivelFormacionId = 3 },
            Cancelacion);
        var problema = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("ProgramaEducativo.ClasificacionInmutable");
    }

    [Fact]
    public async Task Escribir_ComoSuperusuario_Responde403()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var planId = await CrearPlanSimpleAsync(escenario);

        await VerificaEscrituraProhibidaAsync(superusuario, escenario.ProgramaId, planId);
    }

    [Fact]
    public async Task Escribir_ComoEntidadAcademica_Responde403()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var escenario = await EscenarioOferta.CrearAsync(_api, superusuario);
        var planId = await CrearPlanSimpleAsync(escenario);
        using var entidadAcademica = await _api.CrearClienteEntidadAcademicaAsync(escenario.EntidadId);

        await VerificaEscrituraProhibidaAsync(entidadAcademica, escenario.ProgramaId, planId);
    }

    [Fact]
    public async Task Ambito_UnDgaaDeOtraAreaRecibe404AlLeerYAlDarDeBaja_YNoVeLosPlanesAjenos()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var propio = await EscenarioOferta.CrearAsync(_api, superusuario);
        using var ajeno = await EscenarioOferta.CrearAsync(_api, superusuario);
        var planPropio = await CrearPlanSimpleAsync(propio);
        var planAjeno = await CrearPlanSimpleAsync(ajeno);

        using var obtener = await propio.Dgaa.GetAsync(Uri($"/{planAjeno}"), Cancelacion);
        using var experiencias = await propio.Dgaa.GetAsync(Uri($"/{planAjeno}/experiencias-educativas"), Cancelacion);
        using var darDeBaja = await propio.Dgaa.DeleteAsync(Uri($"/{planAjeno}"), Cancelacion);
        using var lista = await propio.Dgaa.GetAsync(Uri(), Cancelacion);
        using var delAjeno = await ajeno.Dgaa.GetAsync(Uri($"/{planAjeno}"), Cancelacion);

        await VerificaNoEncontradoAsync(obtener);
        await VerificaNoEncontradoAsync(experiencias);
        darDeBaja.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        Ids(await EscenarioOferta.Leer(lista)).ShouldBe([planPropio]);
        delAjeno.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ambito_ElSuperusuarioLeeTodosLosPlanes()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var uno = await EscenarioOferta.CrearAsync(_api, superusuario);
        using var otro = await EscenarioOferta.CrearAsync(_api, superusuario);
        var planUno = await CrearPlanSimpleAsync(uno);
        var planOtro = await CrearPlanSimpleAsync(otro);

        await VerificaFiltroAsync(superusuario, $"programaEducativoId={uno.ProgramaId}", planUno);
        await VerificaFiltroAsync(superusuario, $"entidadAcademicaId={otro.EntidadId}", planOtro);
        (await superusuario.GetAsync(Uri($"/{planUno}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await superusuario.GetAsync(Uri($"/{planOtro}/experiencias-educativas"), Cancelacion))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ambito_UnaEntidadAcademicaSoloVeLosPlanesDeSuEntidad()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var propio = await EscenarioOferta.CrearAsync(_api, superusuario);
        using var ajeno = await EscenarioOferta.CrearAsync(_api, superusuario);
        var planPropio = await CrearPlanSimpleAsync(propio);
        var planAjeno = await CrearPlanSimpleAsync(ajeno);
        using var entidadAcademica = await _api.CrearClienteEntidadAcademicaAsync(propio.EntidadId);

        using var obtener = await entidadAcademica.GetAsync(Uri($"/{planPropio}"), Cancelacion);
        using var lista = await entidadAcademica.GetAsync(Uri(), Cancelacion);
        using var experiencias = await entidadAcademica.GetAsync(Uri($"/{planPropio}/experiencias-educativas"), Cancelacion);
        using var ajenoObtener = await entidadAcademica.GetAsync(Uri($"/{planAjeno}"), Cancelacion);
        using var ajenoExperiencias = await entidadAcademica.GetAsync(
            Uri($"/{planAjeno}/experiencias-educativas"), Cancelacion);

        obtener.StatusCode.ShouldBe(HttpStatusCode.OK);
        experiencias.StatusCode.ShouldBe(HttpStatusCode.OK);
        Ids(await EscenarioOferta.Leer(lista)).ShouldBe([planPropio]);
        await VerificaNoEncontradoAsync(ajenoObtener);
        await VerificaNoEncontradoAsync(ajenoExperiencias);
    }

    [Fact]
    public async Task Exportar_ConUnPlan_Responde200ConElArchivoConLosEncabezadosYLaFilaCompleta()
    {
        using var escenario = await NuevoEscenarioAsync();
        var codigo = EscenarioOferta.CodigoDePlan();
        var planId = await EscenarioOferta.CrearPlanAsync(
            escenario.Dgaa,
            escenario.ProgramaId,
            codigo,
            [
                EscenarioOferta.Experiencia(
                    materia: "EXAV", curso: "00001", nombre: "Acreditación del idioma inglés", horasTeoricas: 0, horasPracticas: 0,
                    creditos: 6, areaFormacionId: 3),
                EscenarioOferta.Experiencia(
                    materia: "ENSO", curso: "38003", nombre: "Habilidades de comunicación", horasTeoricas: 2, horasPracticas: 3,
                    creditos: 8, perfilDocente: "Licenciado en informática" + Environment.NewLine + "o afín", areaFormacionId: 1),
            ]);

        using var respuesta = await escenario.Dgaa.GetAsync(Uri($"/{planId}/excel"), Cancelacion);
        using var libro = await AbrirLibroAsync(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        respuesta.Content.Headers.ContentType.ShouldNotBeNull().MediaType
            .ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        respuesta.Content.Headers.ContentDisposition.ShouldNotBeNull().FileName.ShouldNotBeNull().Trim('"')
            .ShouldBe($"{codigo}.xlsx");

        var hoja = libro.Worksheets.Single();
        hoja.Name.ShouldBe("Hoja1");
        Enumerable.Range(1, 14).Select(columna => hoja.Cell(1, columna).GetString()).ShouldBe(EncabezadosDeLaUv);
        hoja.LastRowUsed().ShouldNotBeNull().RowNumber().ShouldBe(3);
        hoja.LastColumnUsed().ShouldNotBeNull().ColumnNumber().ShouldBe(14);

        // Las EE salen en orden de materia y curso: ENSO antes que EXAV.
        hoja.Cell(2, 1).GetString().ShouldBe(escenario.AreaNombre);
        hoja.Cell(2, 2).GetString().ShouldBe(codigo);
        hoja.Cell(2, 3).GetString().ShouldBe(escenario.ProgramaNombre);
        hoja.Cell(2, 4).IsEmpty().ShouldBeTrue();
        hoja.Cell(2, 5).IsEmpty().ShouldBeTrue();
        hoja.Cell(2, 6).GetString().ShouldBe("ENSO");
        hoja.Cell(2, 7).GetString().ShouldBe("38003");
        hoja.Cell(2, 8).GetString().ShouldBe("Habilidades de comunicación");
        VerificaNumero(hoja.Cell(2, 9), 2);
        VerificaNumero(hoja.Cell(2, 10), 3);
        VerificaNumero(hoja.Cell(2, 11), 8);
        hoja.Cell(2, 12).GetString().ShouldBe("111");
        hoja.Cell(2, 13).GetString().ShouldBe("Área de Formación Básica");
        hoja.Cell(2, 14).GetString().ShouldBe("Licenciado en informática" + Environment.NewLine + "o afín");

        // La segunda fila: los ceros iniciales se conservan porque CURSO_EE es texto, y sin perfil la celda queda vacía.
        var curso = hoja.Cell(3, 7);
        curso.DataType.ShouldBe(XLDataType.Text);
        curso.Value.IsText.ShouldBeTrue();
        curso.GetString().ShouldBe("00001");
        hoja.Cell(3, 6).GetString().ShouldBe("EXAV");
        VerificaNumero(hoja.Cell(3, 9), 0);
        VerificaNumero(hoja.Cell(3, 10), 0);
        hoja.Cell(3, 12).GetString().ShouldBe("113");
        hoja.Cell(3, 13).GetString().ShouldBe("Área de Formación Terminal");
        hoja.Cell(3, 14).IsEmpty().ShouldBeTrue();
    }

    [Fact]
    public async Task Exportar_ConElPlanDeEjemploDeLaUv_GeneraUnaFilaPorExperienciaEnOrdenDeMateriaYCurso()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await EscenarioOferta.CrearPlanAsync(
            escenario.Dgaa, escenario.ProgramaId, PlanEjemploIsof14.Codigo, PlanEjemploIsof14.ExperienciasEducativas);

        using var respuesta = await escenario.Dgaa.GetAsync(Uri($"/{planId}/excel"), Cancelacion);
        using var libro = await AbrirLibroAsync(respuesta);
        var hoja = libro.Worksheets.Single();
        var filas = Enumerable.Range(2, 58).ToList();

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        hoja.LastRowUsed().ShouldNotBeNull().RowNumber().ShouldBe(59);
        filas.Select(f => (hoja.Cell(f, 6).GetString(), hoja.Cell(f, 7).GetString()))
            .ShouldBe(PlanEjemploIsof14.ExperienciasEducativas
                .Select(e => (e.Materia, e.Curso))
                .OrderBy(clave => clave.Materia, StringComparer.Ordinal)
                .ThenBy(clave => clave.Curso, StringComparer.Ordinal));
        filas.GroupBy(f => hoja.Cell(f, 12).GetString()).ToDictionary(g => g.Key, g => g.Count())
            .ShouldBe(new Dictionary<string, int> { ["111"] = 18, ["112"] = 22, ["113"] = 18 }, ignoreOrder: true);
        filas.Count(f => hoja.Cell(f, 14).IsEmpty()).ShouldBe(PlanEjemploIsof14.ExperienciasEducativas.Count(e => e.PerfilDocente is null));
        filas.ShouldAllBe(f => hoja.Cell(f, 7).DataType == XLDataType.Text && hoja.Cell(f, 9).DataType == XLDataType.Number);
    }

    [Fact]
    public async Task Exportar_ConUnPlanSinExperienciasActivas_GeneraSoloLaFilaDeEncabezados()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanSimpleAsync(escenario);
        var experienciaId = (await ExperienciasDelPlanAsync(escenario.Dgaa, planId)).Single().Id;
        (await escenario.Dgaa.DeleteAsync(RutaExperiencia(experienciaId), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await escenario.Dgaa.GetAsync(Uri($"/{planId}/excel"), Cancelacion);
        using var libro = await AbrirLibroAsync(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var hoja = libro.Worksheets.Single();
        hoja.LastRowUsed().ShouldNotBeNull().RowNumber().ShouldBe(1);
        Enumerable.Range(1, 14).Select(columna => hoja.Cell(1, columna).GetString()).ShouldBe(EncabezadosDeLaUv);
    }

    [Fact]
    public async Task Exportar_ConUnaExperienciaDadaDeBaja_NoLaIncluye()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await EscenarioOferta.CrearPlanAsync(
            escenario.Dgaa,
            escenario.ProgramaId,
            EscenarioOferta.CodigoDePlan(),
            [EscenarioOferta.Experiencia(materia: "ENSO"), EscenarioOferta.Experiencia(materia: "FBGR")]);
        var dadaDeBaja = (await ExperienciasDelPlanAsync(escenario.Dgaa, planId)).Single(e => e.Materia == "ENSO").Id;
        (await escenario.Dgaa.DeleteAsync(RutaExperiencia(dadaDeBaja), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await escenario.Dgaa.GetAsync(Uri($"/{planId}/excel"), Cancelacion);
        using var libro = await AbrirLibroAsync(respuesta);

        var hoja = libro.Worksheets.Single();
        hoja.LastRowUsed().ShouldNotBeNull().RowNumber().ShouldBe(2);
        hoja.Cell(2, 6).GetString().ShouldBe("FBGR");
    }

    [Fact]
    public async Task Exportar_ConPlanInexistenteODadoDeBaja_Responde404ConCodigo()
    {
        using var escenario = await NuevoEscenarioAsync();
        var planId = await CrearPlanSimpleAsync(escenario);
        (await escenario.Dgaa.DeleteAsync(Uri($"/{planId}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var inexistente = await escenario.Dgaa.GetAsync(Uri($"/{int.MaxValue}/excel"), Cancelacion);
        using var dadoDeBaja = await escenario.Dgaa.GetAsync(Uri($"/{planId}/excel"), Cancelacion);

        await VerificaNoEncontradoAsync(inexistente);
        await VerificaNoEncontradoAsync(dadoDeBaja);
    }

    [Fact]
    public async Task Exportar_ElSuperusuarioYLaEntidadAcademicaExportanLoSuyo_YUnDgaaDeOtraAreaRecibe404()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        using var propio = await EscenarioOferta.CrearAsync(_api, superusuario);
        using var ajeno = await EscenarioOferta.CrearAsync(_api, superusuario);
        var planPropio = await CrearPlanSimpleAsync(propio);
        var planAjeno = await CrearPlanSimpleAsync(ajeno);
        using var entidadAcademica = await _api.CrearClienteEntidadAcademicaAsync(propio.EntidadId);

        using var delSuperusuario = await superusuario.GetAsync(Uri($"/{planAjeno}/excel"), Cancelacion);
        using var delDgaa = await propio.Dgaa.GetAsync(Uri($"/{planPropio}/excel"), Cancelacion);
        using var delaEntidad = await entidadAcademica.GetAsync(Uri($"/{planPropio}/excel"), Cancelacion);
        using var deOtraEntidad = await entidadAcademica.GetAsync(Uri($"/{planAjeno}/excel"), Cancelacion);
        using var deOtraArea = await propio.Dgaa.GetAsync(Uri($"/{planAjeno}/excel"), Cancelacion);

        delSuperusuario.StatusCode.ShouldBe(HttpStatusCode.OK);
        delDgaa.StatusCode.ShouldBe(HttpStatusCode.OK);
        delaEntidad.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var libro = await AbrirLibroAsync(delSuperusuario);
        libro.Worksheets.Single().Cell(2, 1).GetString().ShouldBe(ajeno.AreaNombre);
        await VerificaNoEncontradoAsync(deOtraEntidad);
        await VerificaNoEncontradoAsync(deOtraArea);
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static Uri Uri(string sufijo = "") => new($"{Ruta}{sufijo}", UriKind.Relative);

    private static Uri RutaExperiencia(int id) => new($"/api/v1/oferta-educativa/experiencias-educativas/{id}", UriKind.Relative);

    private static async Task<XLWorkbook> AbrirLibroAsync(HttpResponseMessage respuesta)
    {
        var bytes = await respuesta.Content.ReadAsByteArrayAsync(Cancelacion);
        return new XLWorkbook(new MemoryStream(bytes));
    }

    private static void VerificaNumero(IXLCell celda, double esperado)
    {
        celda.DataType.ShouldBe(XLDataType.Number);
        celda.Value.GetNumber().ShouldBe(esperado);
    }

    private static Uri RutaPrograma(int id) => new($"/api/v1/oferta-educativa/programas-educativos/{id}", UriKind.Relative);

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspaciosRepetidos();

    private static List<int> Ids(JsonElement pagina) =>
        pagina.GetProperty("elementos").EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToList();

    private static object Cuerpo(int programaEducativoId, string codigo, IEnumerable<object> experiencias) =>
        new { programaEducativoId, codigo, experienciasEducativas = experiencias };

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
        problema.GetProperty("codigo").GetString().ShouldBe("PlanEstudios.NoEncontrado");
    }

    /// <summary>Con los filtros dados, el cliente ve exactamente los planes esperados.</summary>
    private static async Task VerificaFiltroAsync(HttpClient cliente, string consulta, params int[] idsEsperados)
    {
        using var respuesta = await cliente.GetAsync(Uri($"?{consulta}"), Cancelacion);
        var pagina = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        Ids(pagina).ShouldBe(idsEsperados, ignoreOrder: true);
    }

    private static async Task VerificaEscrituraProhibidaAsync(HttpClient cliente, int programaId, int planId)
    {
        using var importar = await cliente.PostAsJsonAsync(
            Uri(), Cuerpo(programaId, EscenarioOferta.CodigoDePlan(), [EscenarioOferta.Experiencia()]), Cancelacion);
        using var darDeBaja = await cliente.DeleteAsync(Uri($"/{planId}"), Cancelacion);

        importar.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        darDeBaja.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task VerificaQueNoHayPlanesAsync(EscenarioOferta escenario)
    {
        using var respuesta = await escenario.Dgaa.GetAsync(Uri($"?programaEducativoId={escenario.ProgramaId}"), Cancelacion);
        var pagina = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        pagina.GetProperty("total").GetInt32().ShouldBe(0);
    }

    /// <summary>Un escenario nuevo, con su propio Superusuario que solo se usa para prepararlo.</summary>
    private async Task<EscenarioOferta> NuevoEscenarioAsync()
    {
        using var superusuario = await _api.CrearClienteSuperusuarioAsync();
        return await EscenarioOferta.CrearAsync(_api, superusuario);
    }

    private static Task<int> CrearPlanSimpleAsync(EscenarioOferta escenario) =>
        EscenarioOferta.CrearPlanAsync(
            escenario.Dgaa, escenario.ProgramaId, EscenarioOferta.CodigoDePlan(), [EscenarioOferta.Experiencia()]);

    private static async Task<List<(int Id, string Materia, string Curso)>> ExperienciasDelPlanAsync(HttpClient cliente, int planId)
    {
        using var respuesta = await cliente.GetAsync(Uri($"/{planId}/experiencias-educativas"), Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);

        return (await EscenarioOferta.Leer(respuesta)).EnumerateArray()
            .Select(e => (e.GetProperty("id").GetInt32(), e.GetProperty("materia").GetString()!, e.GetProperty("curso").GetString()!))
            .ToList();
    }
}
