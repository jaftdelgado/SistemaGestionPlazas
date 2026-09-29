using Sgpla.Modules.OfertaEducativa.Application.PeriodosEscolares;
using Sgpla.Modules.OfertaEducativa.Domain.PeriodosEscolares;
using Sgpla.UnitTests.Catalogos.Articulos;

namespace Sgpla.UnitTests.OfertaEducativa.PeriodosEscolares;

public sealed class CrearPeriodoEscolarHandlerTests
{
    private readonly PeriodoEscolarRepositoryFalso _repositorio = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();

    [Fact]
    public async Task HandleAsync_ConDatosValidos_AgregaGuardaYDevuelveLaRespuesta()
    {
        var resultado = await Handler().HandleAsync(Comando(" 202701 "), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Clave.ShouldBe("202701");
        resultado.Value.FechaInicio.ShouldBe(new DateOnly(2026, 8, 10));
        _repositorio.Agregados.Count.ShouldBe(1);
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConDatosInvalidos_FallaSinGuardar()
    {
        var resultado = await Handler().HandleAsync(Comando("abc"), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(PeriodoEscolarErrors.ClaveFormatoInvalido);
        _repositorio.Agregados.ShouldBeEmpty();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConClaveExistente_FallaConClaveDuplicadaSinGuardar()
    {
        _repositorio.ClavesExistentes.Add("202701");

        var resultado = await Handler().HandleAsync(Comando(" 202701 "), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(PeriodoEscolarErrors.ClaveDuplicada);
        _repositorio.Agregados.ShouldBeEmpty();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    private CrearPeriodoEscolarHandler Handler() => new(_repositorio, _unidadDeTrabajo);

    private static CrearPeriodoEscolarCommand Comando(string clave) =>
        new(clave, new DateOnly(2026, 8, 10), new DateOnly(2027, 1, 22));
}
