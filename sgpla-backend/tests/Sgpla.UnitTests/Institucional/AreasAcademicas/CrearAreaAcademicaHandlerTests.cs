using Sgpla.Modules.Institucional.Application.AreasAcademicas;
using Sgpla.Modules.Institucional.Domain.AreasAcademicas;
using Sgpla.UnitTests.Catalogos.Articulos;

namespace Sgpla.UnitTests.Institucional.AreasAcademicas;

public sealed class CrearAreaAcademicaHandlerTests
{
    private readonly AreaAcademicaRepositoryFalso _repositorio = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();

    [Fact]
    public async Task HandleAsync_ConClaveNueva_AgregaYGuarda()
    {
        var resultado = await Handler().HandleAsync(
            new CrearAreaAcademicaCommand(30, "Facultad de Letras", "2288421700", "11350"),
            TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _repositorio.Agregados.ShouldHaveSingleItem().Nombre.ShouldBe("Facultad de Letras");
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConClaveExistente_FallaConClaveDuplicadaSinGuardar()
    {
        _repositorio.ClavesExistentes.Add(30);

        var resultado = await Handler().HandleAsync(
            new CrearAreaAcademicaCommand(30, "Facultad de Letras", "2288421700", null),
            TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(AreaAcademicaErrors.ClaveDuplicada);
        _repositorio.Agregados.ShouldBeEmpty();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConDatosInvalidos_NoConsultaElRepositorioNiGuarda()
    {
        var resultado = await Handler().HandleAsync(
            new CrearAreaAcademicaCommand(0, "Facultad de Letras", "2288421700", null),
            TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(AreaAcademicaErrors.ClaveNoPositiva);
        _repositorio.Agregados.ShouldBeEmpty();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    private CrearAreaAcademicaHandler Handler() => new(_repositorio, _unidadDeTrabajo);
}
