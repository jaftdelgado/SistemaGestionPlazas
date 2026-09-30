using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Application.PlanesEstudio;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;
using Sgpla.UnitTests.Catalogos.Articulos;
using Sgpla.UnitTests.Institucional;

namespace Sgpla.UnitTests.OfertaEducativa.PlanesEstudio;

public sealed class DarDeBajaPlanEstudiosHandlerTests
{
    private const int Id = 5;
    private const int EntidadId = 7;

    private readonly PlanEstudiosRepositoryFalso _repositorio = new();
    private readonly AmbitoOfertaEducativaFalso _ambito = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();
    private readonly PlanEstudios _plan = PlanEstudios.Crear(
        "ISOF-14",
        12,
        [
            new DatosExperienciaEducativa("Uno", "ENSO", "38003", 2, 2, 6, null, null, null, 1),
            new DatosExperienciaEducativa("Dos", "EXAV", "00001", 0, 0, 6, null, null, null, 3),
        ]).Value;

    public DarDeBajaPlanEstudiosHandlerTests()
    {
        _repositorio.Registrar(Id, _plan);
        _repositorio.EntidadDelPlan = EntidadId;
        _ambito.EntidadesEscribibles.Add(EntidadId);
    }

    [Fact]
    public async Task HandleAsync_SinBloqueos_DaDeBajaElPlanYSusExperienciasConElMismoInstanteTruncadoYGuarda()
    {
        var ahoraConMilisegundos = new DateTimeOffset(2026, 3, 15, 10, 30, 45, 250, TimeSpan.Zero);
        var esperado = new DateTime(2026, 3, 15, 10, 30, 45, DateTimeKind.Utc);

        var resultado = await Handler(ahoraConMilisegundos).HandleAsync(
            new DarDeBajaPlanEstudiosCommand(Id), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _plan.FechaEliminacion.ShouldBe(esperado);
        _plan.ExperienciasEducativas.ShouldAllBe(e => e.FechaEliminacion == esperado);
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConReferenciasSinBloqueo_ConsultaCadaImplementacionConLasExperienciasDelPlan()
    {
        var primera = new ReferenciasExperienciaEducativaFalsas(false);
        var segunda = new ReferenciasExperienciaEducativaFalsas(false);

        var resultado = await Handler(DateTimeOffset.UtcNow, primera, segunda).HandleAsync(
            new DarDeBajaPlanEstudiosCommand(Id), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        primera.Consultas.ShouldHaveSingleItem().Count.ShouldBe(2);
        segunda.Consultas.ShouldHaveSingleItem().Count.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_ConPlanInexistente_FallaConNoEncontradoSinGuardar()
    {
        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaPlanEstudiosCommand(Id + 1), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(PlanEstudiosErrors.NoEncontrado(Id + 1));
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConPlanDeOtraArea_FallaConNoEncontradoSinGuardar()
    {
        _ambito.EntidadesEscribibles.Clear();

        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaPlanEstudiosCommand(Id), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(PlanEstudiosErrors.NoEncontrado(Id));
        _plan.FechaEliminacion.ShouldBeNull();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConProgramacionesActivas_FallaConExperienciasConProgramacionesActivasSinGuardar()
    {
        _repositorio.TieneExperienciasConProgramacionesActivas = true;
        var referencias = new ReferenciasExperienciaEducativaFalsas(false);

        var resultado = await Handler(DateTimeOffset.UtcNow, referencias).HandleAsync(
            new DarDeBajaPlanEstudiosCommand(Id), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(PlanEstudiosErrors.ExperienciasConProgramacionesActivas);
        _plan.FechaEliminacion.ShouldBeNull();
        _plan.ExperienciasEducativas.ShouldAllBe(e => e.FechaEliminacion == null);
        referencias.Consultas.ShouldBeEmpty();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConReferenciasDeOtroModulo_FallaConTieneReferenciasSinGuardar()
    {
        var resultado = await Handler(DateTimeOffset.UtcNow, new ReferenciasExperienciaEducativaFalsas(true)).HandleAsync(
            new DarDeBajaPlanEstudiosCommand(Id), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ExperienciaEducativaErrors.TieneReferencias);
        _plan.FechaEliminacion.ShouldBeNull();
        _plan.ExperienciasEducativas.ShouldAllBe(e => e.FechaEliminacion == null);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    private DarDeBajaPlanEstudiosHandler Handler(DateTimeOffset ahora, params IReferenciasExperienciaEducativa[] referencias) =>
        new(_repositorio, _ambito, referencias, _unidadDeTrabajo, new TimeProviderFalso(ahora));
}
