using Sgpla.Modules.OfertaEducativa.Application.ProgramasEducativos;
using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;
using Sgpla.UnitTests.Catalogos.Articulos;
using Sgpla.UnitTests.Institucional;

namespace Sgpla.UnitTests.OfertaEducativa.ProgramasEducativos;

public sealed class DarDeBajaProgramaEducativoHandlerTests
{
    private const int Id = 12;
    private const int EntidadId = 7;

    private readonly ProgramaEducativoRepositoryFalso _repositorio = new();
    private readonly AmbitoOfertaEducativaFalso _ambito = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();
    private readonly ProgramaEducativo _programa = ProgramaEducativo.Crear("Ingeniería de Software", EntidadId, 1, 3).Value;

    public DarDeBajaProgramaEducativoHandlerTests()
    {
        _repositorio.Registrar(Id, _programa);
        _ambito.EntidadesEscribibles.Add(EntidadId);
    }

    [Fact]
    public async Task HandleAsync_SinPlanesActivos_DaDeBajaConElInstanteTruncadoYGuarda()
    {
        var ahoraConMilisegundos = new DateTimeOffset(2026, 3, 15, 10, 30, 45, 250, TimeSpan.Zero);

        var resultado = await Handler(ahoraConMilisegundos).HandleAsync(
            new DarDeBajaProgramaEducativoCommand(Id), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _programa.FechaEliminacion.ShouldBe(new DateTime(2026, 3, 15, 10, 30, 45, DateTimeKind.Utc));
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConPlanesSoloDadosDeBaja_DaDeBaja()
    {
        _repositorio.TuvoPlanes = true;
        _repositorio.TienePlanesActivos = false;

        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaProgramaEducativoCommand(Id), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConPlanesActivos_FallaConTienePlanesActivosSinGuardar()
    {
        _repositorio.TuvoPlanes = true;
        _repositorio.TienePlanesActivos = true;

        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaProgramaEducativoCommand(Id), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ProgramaEducativoErrors.TienePlanesActivos);
        _programa.FechaEliminacion.ShouldBeNull();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConProgramaInexistente_FallaConNoEncontradoSinGuardar()
    {
        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaProgramaEducativoCommand(Id + 1), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ProgramaEducativoErrors.NoEncontrado(Id + 1));
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConProgramaDeOtraArea_FallaConNoEncontradoSinGuardar()
    {
        _ambito.EntidadesEscribibles.Clear();

        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaProgramaEducativoCommand(Id), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ProgramaEducativoErrors.NoEncontrado(Id));
        _programa.FechaEliminacion.ShouldBeNull();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    private DarDeBajaProgramaEducativoHandler Handler(DateTimeOffset ahora) =>
        new(_repositorio, _ambito, _unidadDeTrabajo, new TimeProviderFalso(ahora));
}
