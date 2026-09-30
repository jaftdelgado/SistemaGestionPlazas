using Sgpla.Modules.OfertaEducativa.Application.ExperienciasEducativas;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;
using Sgpla.UnitTests.Catalogos.Articulos;

namespace Sgpla.UnitTests.OfertaEducativa.ExperienciasEducativas;

public sealed class CrearExperienciaEducativaHandlerTests
{
    private const int PlanId = 5;
    private const int EntidadId = 7;

    private readonly PlanEstudiosRepositoryFalso _planes = new();
    private readonly ExperienciaEducativaRepositoryFalso _repositorio = new();
    private readonly AmbitoOfertaEducativaFalso _ambito = new();
    private readonly ClasificacionesAcademicasFalso _clasificaciones = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();
    private readonly PlanEstudios _plan = PlanEstudios.Crear(
        "ISOF-14",
        12,
        [new DatosExperienciaEducativa("Existente", "FBGR", "80001", 2, 2, 6, null, null, null, 1)]).Value;

    public CrearExperienciaEducativaHandlerTests()
    {
        _planes.Registrar(PlanId, _plan);
        _planes.EntidadDelPlan = EntidadId;
        _ambito.EntidadesEscribibles.Add(EntidadId);
        _clasificaciones.Areas.UnionWith([1, 2, 3]);
    }

    [Fact]
    public async Task HandleAsync_ConDatosValidos_AgregaLaExperienciaNormalizadaAlPlanYGuardaUnaVez()
    {
        var resultado = await Handler().HandleAsync(
            Comando(Entrada() with { Materia = " enso ", Curso = "00001", Nombre = "  Habilidades   de comunicación " }),
            TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _plan.ExperienciasEducativas.Count.ShouldBe(2);
        var agregada = _plan.ExperienciasEducativas.Last();
        agregada.Materia.ShouldBe("ENSO");
        agregada.Curso.ShouldBe("00001");
        agregada.Nombre.ShouldBe("Habilidades de comunicación");
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConPlanInexistente_FallaConPlanEstudiosInexistenteSinGuardar()
    {
        var resultado = await Handler().HandleAsync(
            new CrearExperienciaEducativaCommand(PlanId + 1, Entrada()), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ExperienciaEducativaErrors.PlanEstudiosInexistente);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConPlanDeOtraArea_FallaConPlanEstudiosInexistenteSinGuardar()
    {
        _ambito.EntidadesEscribibles.Clear();

        var resultado = await Handler().HandleAsync(Comando(Entrada()), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ExperienciaEducativaErrors.PlanEstudiosInexistente);
        _plan.ExperienciasEducativas.Count.ShouldBe(1);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConDatosInvalidos_FallaConElErrorDelDominioSinGuardar()
    {
        var resultado = await Handler().HandleAsync(
            Comando(Entrada() with { Creditos = 0 }), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ExperienciaEducativaErrors.CreditosNoPositivos);
        resultado.Error.Campo.ShouldBe("Creditos");
        _plan.ExperienciasEducativas.Count.ShouldBe(1);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConAreaInexistente_FallaConAreaFormacionInexistenteSinGuardar()
    {
        var resultado = await Handler().HandleAsync(
            Comando(Entrada() with { AreaFormacionId = 99 }), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ExperienciaEducativaErrors.AreaFormacionInexistente);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConMateriaYCursoExistentesEnElPlan_FallaConMateriaCursoDuplicadoSinGuardar()
    {
        _repositorio.MateriasYCursosExistentes.Add(("ENSO", "38003"));

        var resultado = await Handler().HandleAsync(
            Comando(Entrada() with { Materia = "enso", Curso = "38003" }), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ExperienciaEducativaErrors.MateriaCursoDuplicado);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    private static CrearExperienciaEducativaCommand Comando(ExperienciaEducativaEntrada entrada) => new(PlanId, entrada);

    private static ExperienciaEducativaEntrada Entrada() =>
        new("Habilidades de comunicación", "ENSO", "38003", 2, 2, 6, null, null, null, 1);

    private CrearExperienciaEducativaHandler Handler() =>
        new(_planes, _repositorio, _ambito, _clasificaciones, _unidadDeTrabajo);
}
