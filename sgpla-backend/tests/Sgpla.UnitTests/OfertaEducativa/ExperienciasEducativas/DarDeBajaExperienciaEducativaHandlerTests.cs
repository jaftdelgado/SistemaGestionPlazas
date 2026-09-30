using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Application.ExperienciasEducativas;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;
using Sgpla.UnitTests.Catalogos.Articulos;
using Sgpla.UnitTests.Institucional;

namespace Sgpla.UnitTests.OfertaEducativa.ExperienciasEducativas;

public sealed class DarDeBajaExperienciaEducativaHandlerTests
{
    private const int Id = 8;
    private const int EntidadId = 7;

    private readonly ExperienciaEducativaRepositoryFalso _repositorio = new();
    private readonly AmbitoOfertaEducativaFalso _ambito = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();
    private readonly ExperienciaEducativa _experiencia = ExperienciaEducativa.Crear(
        new DatosExperienciaEducativa("Habilidades", "ENSO", "38003", 2, 3, 6, null, null, null, 1)).Value;

    public DarDeBajaExperienciaEducativaHandlerTests()
    {
        _repositorio.Registrar(Id, _experiencia);
        _repositorio.EntidadDeLaExperiencia = EntidadId;
        _ambito.EntidadesEscribibles.Add(EntidadId);
    }

    [Fact]
    public async Task HandleAsync_SinBloqueos_DaDeBajaConElInstanteTruncadoYGuarda()
    {
        var ahoraConMilisegundos = new DateTimeOffset(2026, 3, 15, 10, 30, 45, 250, TimeSpan.Zero);

        var resultado = await Handler(ahoraConMilisegundos).HandleAsync(
            new DarDeBajaExperienciaEducativaCommand(Id), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _experiencia.FechaEliminacion.ShouldBe(new DateTime(2026, 3, 15, 10, 30, 45, DateTimeKind.Utc));
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConProgramacionesSoloDadasDeBaja_DaDeBaja()
    {
        _repositorio.TuvoProgramaciones = true;
        _repositorio.TieneProgramacionesActivas = false;

        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaExperienciaEducativaCommand(Id), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConProgramacionesActivas_FallaConTieneProgramacionesActivasSinGuardar()
    {
        _repositorio.TieneProgramacionesActivas = true;
        var referencias = new ReferenciasExperienciaEducativaFalsas(false);

        var resultado = await Handler(DateTimeOffset.UtcNow, referencias).HandleAsync(
            new DarDeBajaExperienciaEducativaCommand(Id), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ExperienciaEducativaErrors.TieneProgramacionesActivas);
        _experiencia.FechaEliminacion.ShouldBeNull();
        referencias.Consultas.ShouldBeEmpty();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConReferenciasDeOtroModulo_FallaConTieneReferenciasSinGuardar()
    {
        var sinReferencias = new ReferenciasExperienciaEducativaFalsas(false);
        var conReferencias = new ReferenciasExperienciaEducativaFalsas(true);

        var resultado = await Handler(DateTimeOffset.UtcNow, sinReferencias, conReferencias).HandleAsync(
            new DarDeBajaExperienciaEducativaCommand(Id), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ExperienciaEducativaErrors.TieneReferencias);
        sinReferencias.Consultas.ShouldHaveSingleItem().Count.ShouldBe(1);
        conReferencias.Consultas.ShouldHaveSingleItem().Count.ShouldBe(1);
        _experiencia.FechaEliminacion.ShouldBeNull();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConReferenciasSinBloqueo_DaDeBaja()
    {
        var referencias = new ReferenciasExperienciaEducativaFalsas(false);

        var resultado = await Handler(DateTimeOffset.UtcNow, referencias).HandleAsync(
            new DarDeBajaExperienciaEducativaCommand(Id), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        referencias.Consultas.ShouldHaveSingleItem().Count.ShouldBe(1);
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConExperienciaInexistente_FallaConNoEncontradoSinGuardar()
    {
        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaExperienciaEducativaCommand(Id + 1), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ExperienciaEducativaErrors.NoEncontrado(Id + 1));
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConExperienciaDeOtraArea_FallaConNoEncontradoSinGuardar()
    {
        _ambito.EntidadesEscribibles.Clear();

        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaExperienciaEducativaCommand(Id), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ExperienciaEducativaErrors.NoEncontrado(Id));
        _experiencia.FechaEliminacion.ShouldBeNull();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    private DarDeBajaExperienciaEducativaHandler Handler(DateTimeOffset ahora, params IReferenciasExperienciaEducativa[] referencias) =>
        new(_repositorio, _ambito, referencias, _unidadDeTrabajo, new TimeProviderFalso(ahora));
}
