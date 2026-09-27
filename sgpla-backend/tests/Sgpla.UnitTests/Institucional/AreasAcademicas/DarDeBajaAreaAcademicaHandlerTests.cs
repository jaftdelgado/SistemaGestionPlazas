using Sgpla.Modules.Institucional.Application.AreasAcademicas;
using Sgpla.Modules.Institucional.Domain.AreasAcademicas;
using Sgpla.UnitTests.Catalogos.Articulos;

namespace Sgpla.UnitTests.Institucional.AreasAcademicas;

public sealed class DarDeBajaAreaAcademicaHandlerTests
{
    private const int Id = 7;

    private readonly AreaAcademicaRepositoryFalso _repositorio = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();
    private readonly AreaAcademica _area = AreaAcademica.Crear(30, "Facultad de Letras", "2288421700", null).Value;

    public DarDeBajaAreaAcademicaHandlerTests() => _repositorio.Registrar(Id, _area);

    [Fact]
    public async Task HandleAsync_SinEntidadesActivas_DaDeBajaYGuarda()
    {
        var ahoraConMilisegundos = new DateTimeOffset(2026, 3, 15, 10, 30, 45, 250, TimeSpan.Zero);

        var resultado = await Handler(ahoraConMilisegundos).HandleAsync(
            new DarDeBajaAreaAcademicaCommand(Id), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _area.FechaEliminacion.ShouldBe(new DateTime(2026, 3, 15, 10, 30, 45, DateTimeKind.Utc));
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConEntidadesActivas_FallaConTieneEntidadesActivasSinGuardar()
    {
        _repositorio.TieneEntidadesActivas = true;

        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaAreaAcademicaCommand(Id), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(AreaAcademicaErrors.TieneEntidadesActivas);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConAreaInexistente_FallaConNoEncontradoSinGuardar()
    {
        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaAreaAcademicaCommand(Id + 1), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(AreaAcademicaErrors.NoEncontrado(Id + 1));
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    private DarDeBajaAreaAcademicaHandler Handler(DateTimeOffset ahora) =>
        new(_repositorio, _unidadDeTrabajo, new TimeProviderFalso(ahora));
}
