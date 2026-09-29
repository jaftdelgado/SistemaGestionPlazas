using Sgpla.Modules.OfertaEducativa.Application.PeriodosEscolares;
using Sgpla.Modules.OfertaEducativa.Domain.PeriodosEscolares;
using Sgpla.UnitTests.Catalogos.Articulos;

namespace Sgpla.UnitTests.OfertaEducativa.PeriodosEscolares;

public sealed class ModificarPeriodoEscolarHandlerTests
{
    private const int Id = 4;

    private static readonly DateOnly Inicio = new(2026, 8, 10);
    private static readonly DateOnly Fin = new(2027, 1, 22);

    private readonly PeriodoEscolarRepositoryFalso _repositorio = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();
    private readonly PeriodoEscolar _periodo = PeriodoEscolar.Crear("202701", Inicio, Fin).Value;

    public ModificarPeriodoEscolarHandlerTests() => _repositorio.Registrar(Id, _periodo);

    [Fact]
    public async Task HandleAsync_ConRangoValido_ModificaYGuarda()
    {
        var nuevoFin = new DateOnly(2027, 2, 5);

        var resultado = await Handler().HandleAsync(
            new ModificarPeriodoEscolarCommand(Id, Inicio, nuevoFin), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _periodo.FechaFin.ShouldBe(nuevoFin);
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConProgramaciones_ModificaLasFechasDeTodosModos()
    {
        _repositorio.TieneProgramaciones = true;

        var resultado = await Handler().HandleAsync(
            new ModificarPeriodoEscolarCommand(Id, Inicio, Fin.AddDays(7)), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConRangoInvalido_FallaSinGuardarNiCambiarNada()
    {
        var resultado = await Handler().HandleAsync(
            new ModificarPeriodoEscolarCommand(Id, Fin, Inicio), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(PeriodoEscolarErrors.RangoFechasInvalido);
        _periodo.FechaInicio.ShouldBe(Inicio);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConPeriodoInexistente_FallaConNoEncontradoSinGuardar()
    {
        var resultado = await Handler().HandleAsync(
            new ModificarPeriodoEscolarCommand(Id + 1, Inicio, Fin), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(PeriodoEscolarErrors.NoEncontrado(Id + 1));
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    private ModificarPeriodoEscolarHandler Handler() => new(_repositorio, _unidadDeTrabajo);
}
