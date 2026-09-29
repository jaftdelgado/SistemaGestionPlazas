using Sgpla.Modules.OfertaEducativa.Application.ExperienciasEducativas;
using Sgpla.Modules.OfertaEducativa.Application.PlanesEstudio;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;
using Sgpla.UnitTests.Catalogos.Articulos;

namespace Sgpla.UnitTests.OfertaEducativa.PlanesEstudio;

public sealed class ImportarPlanEstudiosHandlerTests
{
    private const int ProgramaId = 12;
    private const int EntidadId = 7;

    private readonly PlanEstudiosRepositoryFalso _repositorio = new();
    private readonly AmbitoOfertaEducativaFalso _ambito = new();
    private readonly ClasificacionesAcademicasFalso _clasificaciones = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();

    public ImportarPlanEstudiosHandlerTests()
    {
        _repositorio.EntidadesDeProgramasActivos[ProgramaId] = EntidadId;
        _ambito.EntidadesEscribibles.Add(EntidadId);
        _clasificaciones.Areas.UnionWith([1, 2, 3]);
    }

    [Fact]
    public async Task HandleAsync_ConDatosValidos_AgregaElPlanConSusExperienciasYGuardaUnaVez()
    {
        var resultado = await Handler().HandleAsync(
            Comando(" isof-14-e-cr ", Experiencia("ENSO", "38003"), Experiencia("EXAV", "00001", areaId: 3)),
            TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        var plan = _repositorio.Agregados.ShouldHaveSingleItem();
        plan.Codigo.ShouldBe("ISOF-14-E-CR");
        plan.ProgramaEducativoId.ShouldBe(ProgramaId);
        plan.ExperienciasEducativas.Select(e => (e.Materia, e.Curso)).ShouldBe([("ENSO", "38003"), ("EXAV", "00001")]);
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConAreasRepetidas_ConsultaLasAreasUnaSolaVez()
    {
        var resultado = await Handler().HandleAsync(
            Comando("ISOF-14", Experiencia("A", "1"), Experiencia("B", "1"), Experiencia("C", "1", areaId: 2)),
            TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _clasificaciones.ConsultasDeAreas.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConCodigoInvalido_FallaSinGuardar()
    {
        var resultado = await Handler().HandleAsync(
            Comando("ISOF_14", Experiencia("ENSO", "38003")), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(PlanEstudiosErrors.CodigoFormatoInvalido);
        VerificaQueNoGuardo();
    }

    [Fact]
    public async Task HandleAsync_SinExperiencias_FallaConSinExperiencias()
    {
        var resultado = await Handler().HandleAsync(Comando("ISOF-14"), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(PlanEstudiosErrors.SinExperiencias);
        VerificaQueNoGuardo();
    }

    [Fact]
    public async Task HandleAsync_ConUnaExperienciaInvalida_FallaConElIndiceEnElCampoSinGuardar()
    {
        var resultado = await Handler().HandleAsync(
            Comando("ISOF-14", Experiencia("A", "1"), Experiencia("B", "1"), Experiencia("C", "1", creditos: 0)),
            TestContext.Current.CancellationToken);

        resultado.Error.Code.ShouldBe(ExperienciaEducativaErrors.CreditosNoPositivos.Code);
        resultado.Error.Campo.ShouldBe("ExperienciasEducativas[2].Creditos");
        VerificaQueNoGuardo();
    }

    [Fact]
    public async Task HandleAsync_ConFormaInvalidaYProgramaFueraDelAmbito_DevuelveElErrorDeLaForma()
    {
        _ambito.EntidadesEscribibles.Clear();

        var resultado = await Handler().HandleAsync(
            Comando("ISOF-14", Experiencia("A", "1", creditos: 0)), TestContext.Current.CancellationToken);

        resultado.Error.Campo.ShouldBe("ExperienciasEducativas[0].Creditos");
        VerificaQueNoGuardo();
    }

    [Fact]
    public async Task HandleAsync_ConProgramaInexistenteODadoDeBaja_FallaConProgramaEducativoInexistente()
    {
        var resultado = await Handler().HandleAsync(
            Comando("ISOF-14", Experiencia("A", "1")) with { ProgramaEducativoId = ProgramaId + 1 },
            TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(PlanEstudiosErrors.ProgramaEducativoInexistente);
        VerificaQueNoGuardo();
    }

    [Fact]
    public async Task HandleAsync_ConProgramaDeOtraArea_FallaConProgramaEducativoInexistente()
    {
        _ambito.EntidadesEscribibles.Clear();

        var resultado = await Handler().HandleAsync(
            Comando("ISOF-14", Experiencia("A", "1")), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(PlanEstudiosErrors.ProgramaEducativoInexistente);
        VerificaQueNoGuardo();
    }

    [Fact]
    public async Task HandleAsync_ConAreaInexistenteEnLaSegundaExperiencia_FallaConElIndiceSinGuardar()
    {
        var resultado = await Handler().HandleAsync(
            Comando(
                "ISOF-14",
                Experiencia("A", "1"),
                Experiencia("B", "1", areaId: 99),
                Experiencia("C", "1", areaId: 98)),
            TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(PlanEstudiosErrors.AreaFormacionInexistente(1));
        resultado.Error.Campo.ShouldBe("ExperienciasEducativas[1].AreaFormacionId");
        VerificaQueNoGuardo();
    }

    [Fact]
    public async Task HandleAsync_ConCodigoYaUsadoEnElPrograma_FallaConCodigoDuplicado()
    {
        _repositorio.CodigosExistentes.Add((ProgramaId, "ISOF-14"));

        var resultado = await Handler().HandleAsync(
            Comando("isof-14", Experiencia("A", "1")), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(PlanEstudiosErrors.CodigoDuplicado);
        VerificaQueNoGuardo();
    }

    [Fact]
    public async Task HandleAsync_ConElMismoCodigoEnOtroPrograma_LoAcepta()
    {
        _repositorio.CodigosExistentes.Add((ProgramaId + 1, "ISOF-14"));

        var resultado = await Handler().HandleAsync(
            Comando("ISOF-14", Experiencia("A", "1")), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    private static ImportarPlanEstudiosCommand Comando(string codigo, params ExperienciaEducativaEntrada[] experiencias) =>
        new(ProgramaId, codigo, experiencias);

    private static ExperienciaEducativaEntrada Experiencia(string materia, string curso, int creditos = 6, int areaId = 1) =>
        new("Experiencia educativa", materia, curso, 2, 2, creditos, null, null, null, areaId);

    private ImportarPlanEstudiosHandler Handler() => new(_repositorio, _ambito, _clasificaciones, _unidadDeTrabajo);

    private void VerificaQueNoGuardo()
    {
        _repositorio.Agregados.ShouldBeEmpty();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }
}
