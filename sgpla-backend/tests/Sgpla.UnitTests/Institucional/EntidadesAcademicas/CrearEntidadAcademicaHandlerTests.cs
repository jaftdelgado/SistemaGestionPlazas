using Sgpla.Modules.Institucional.Application.EntidadesAcademicas;
using Sgpla.Modules.Institucional.Domain.EntidadesAcademicas;
using Sgpla.UnitTests.Catalogos.Articulos;

namespace Sgpla.UnitTests.Institucional.EntidadesAcademicas;

public sealed class CrearEntidadAcademicaHandlerTests
{
    private readonly EntidadAcademicaRepositoryFalso _repositorio = new();
    private readonly MunicipiosFalso _municipios = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();

    public CrearEntidadAcademicaHandlerTests()
    {
        _repositorio.CamposExistentes.Add(1);
        _repositorio.AreasAcademicasActivas.Add(3);
        _municipios.Existentes[87] = "Xalapa";
    }

    [Fact]
    public async Task HandleAsync_ConDatosValidos_AgregaYGuardaYDevuelveElId()
    {
        var resultado = await Handler().HandleAsync(Comando(), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _repositorio.Agregados.ShouldHaveSingleItem().Clave.ShouldBe("FLX1");
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConCampusInexistente_FallaConCampusInexistenteSinGuardar()
    {
        var resultado = await Handler().HandleAsync(Comando(campusId: 99), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(EntidadAcademicaErrors.CampusInexistente);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConAreaAcademicaInexistente_FallaConAreaAcademicaInexistenteSinGuardar()
    {
        var resultado = await Handler().HandleAsync(Comando(areaAcademicaId: 99), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(EntidadAcademicaErrors.AreaAcademicaInexistente);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConMunicipioInexistente_FallaConMunicipioInexistenteSinGuardar()
    {
        var resultado = await Handler().HandleAsync(Comando(municipioId: 99), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(EntidadAcademicaErrors.MunicipioInexistente);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConClaveExistente_FallaConClaveDuplicadaSinGuardar()
    {
        _repositorio.ClavesExistentes.Add("FLX1");

        var resultado = await Handler().HandleAsync(Comando(), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(EntidadAcademicaErrors.ClaveDuplicada);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    private CrearEntidadAcademicaHandler Handler() => new(_repositorio, _municipios, _unidadDeTrabajo);

    private static CrearEntidadAcademicaCommand Comando(int campusId = 1, int areaAcademicaId = 3, int municipioId = 87) =>
        new("FLX1", "Facultad de Letras", "Calle", null, "Colonia", "91020", "2288421700", null, campusId, areaAcademicaId, municipioId);
}
