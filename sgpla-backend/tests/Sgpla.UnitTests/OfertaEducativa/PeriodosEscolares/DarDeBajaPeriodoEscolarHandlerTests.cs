using Sgpla.Modules.OfertaEducativa.Application.PeriodosEscolares;
using Sgpla.Modules.OfertaEducativa.Domain.PeriodosEscolares;
using Sgpla.UnitTests.Catalogos.Articulos;
using Sgpla.UnitTests.Institucional;

namespace Sgpla.UnitTests.OfertaEducativa.PeriodosEscolares;

public sealed class DarDeBajaPeriodoEscolarHandlerTests
{
    private const int Id = 4;

    private readonly PeriodoEscolarRepositoryFalso _repositorio = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();
    private readonly PeriodoEscolar _periodo =
        PeriodoEscolar.Crear("202701", new DateOnly(2026, 8, 10), new DateOnly(2027, 1, 22)).Value;

    public DarDeBajaPeriodoEscolarHandlerTests() => _repositorio.Registrar(Id, _periodo);

    [Fact]
    public async Task HandleAsync_SinReferencias_DaDeBajaConElInstanteTruncadoYGuarda()
    {
        var ahoraConMilisegundos = new DateTimeOffset(2026, 3, 15, 10, 30, 45, 250, TimeSpan.Zero);
        var referencias = new ReferenciasPeriodoEscolarFalsas(false);

        var resultado = await Handler(ahoraConMilisegundos, referencias).HandleAsync(
            new DarDeBajaPeriodoEscolarCommand(Id), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _periodo.FechaEliminacion.ShouldBe(new DateTime(2026, 3, 15, 10, 30, 45, DateTimeKind.Utc));
        referencias.Consultas.ShouldBe(1);
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_SinImplementacionesDeReferencias_DaDeBaja()
    {
        var resultado = await new DarDeBajaPeriodoEscolarHandler(
            _repositorio, [], _unidadDeTrabajo, new TimeProviderFalso(DateTimeOffset.UtcNow))
            .HandleAsync(new DarDeBajaPeriodoEscolarCommand(Id), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConProgramaciones_FallaConTieneReferenciasSinConsultarOtrosModulosNiGuardar()
    {
        _repositorio.TieneProgramaciones = true;
        var referencias = new ReferenciasPeriodoEscolarFalsas(false);

        var resultado = await Handler(DateTimeOffset.UtcNow, referencias).HandleAsync(
            new DarDeBajaPeriodoEscolarCommand(Id), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(PeriodoEscolarErrors.TieneReferencias);
        referencias.Consultas.ShouldBe(0);
        _periodo.FechaEliminacion.ShouldBeNull();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConReferenciaDeOtroModulo_FallaConTieneReferenciasSinGuardar()
    {
        var sinReferencias = new ReferenciasPeriodoEscolarFalsas(false);
        var conReferencias = new ReferenciasPeriodoEscolarFalsas(true);

        var resultado = await new DarDeBajaPeriodoEscolarHandler(
            _repositorio, [sinReferencias, conReferencias], _unidadDeTrabajo, new TimeProviderFalso(DateTimeOffset.UtcNow))
            .HandleAsync(new DarDeBajaPeriodoEscolarCommand(Id), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(PeriodoEscolarErrors.TieneReferencias);
        _periodo.FechaEliminacion.ShouldBeNull();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConPeriodoInexistente_FallaConNoEncontradoSinGuardar()
    {
        var resultado = await Handler(DateTimeOffset.UtcNow, new ReferenciasPeriodoEscolarFalsas(false)).HandleAsync(
            new DarDeBajaPeriodoEscolarCommand(Id + 1), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(PeriodoEscolarErrors.NoEncontrado(Id + 1));
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    private DarDeBajaPeriodoEscolarHandler Handler(DateTimeOffset ahora, ReferenciasPeriodoEscolarFalsas referencias) =>
        new(_repositorio, [referencias], _unidadDeTrabajo, new TimeProviderFalso(ahora));
}
