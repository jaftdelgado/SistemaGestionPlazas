using Sgpla.Modules.Institucional.Application.AreasAcademicas;
using Sgpla.Modules.Institucional.Domain.AreasAcademicas;
using Sgpla.UnitTests.Catalogos.Articulos;

namespace Sgpla.UnitTests.Institucional.AreasAcademicas;

public sealed class ModificarAreaAcademicaHandlerTests
{
    private const int Id = 7;

    private readonly AreaAcademicaRepositoryFalso _repositorio = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();
    private readonly AreaAcademica _area = AreaAcademica.Crear(30, "Facultad de Letras", "2288421700", null).Value;

    public ModificarAreaAcademicaHandlerTests() => _repositorio.Registrar(Id, _area);

    [Fact]
    public async Task HandleAsync_ConDatosValidos_ModificaYGuarda()
    {
        var resultado = await Handler().HandleAsync(
            new ModificarAreaAcademicaCommand(Id, "Facultad de Física", "2288421701", "11350"),
            TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _area.Nombre.ShouldBe("Facultad de Física");
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConDatosInvalidos_NoCambiaNiGuarda()
    {
        var resultado = await Handler().HandleAsync(
            new ModificarAreaAcademicaCommand(Id, "", "2288421701", null),
            TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(AreaAcademicaErrors.NombreVacio);
        _area.Nombre.ShouldBe("Facultad de Letras");
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConAreaInexistente_FallaConNoEncontradoSinGuardar()
    {
        var resultado = await Handler().HandleAsync(
            new ModificarAreaAcademicaCommand(Id + 1, "Facultad de Física", "2288421701", null),
            TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(AreaAcademicaErrors.NoEncontrado(Id + 1));
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    private ModificarAreaAcademicaHandler Handler() => new(_repositorio, _unidadDeTrabajo);
}
