using Sgpla.Modules.Institucional.Application.EntidadesAcademicas;
using Sgpla.Modules.Institucional.Domain.EntidadesAcademicas;
using Sgpla.UnitTests.Catalogos.Articulos;

namespace Sgpla.UnitTests.Institucional.EntidadesAcademicas;

public sealed class ModificarEntidadAcademicaHandlerTests
{
    private const int Id = 7;

    private readonly EntidadAcademicaRepositoryFalso _repositorio = new();
    private readonly MunicipiosFalso _municipios = new();
    private readonly ProgramasDeEntidadAcademicaFalso _programas = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();
    private readonly EntidadAcademica _entidad = EntidadAcademica.Crear(
        "FLX1", "Facultad de Letras", "Calle", null, "Colonia", "91020", "2288421700", null, 1, 3, 87).Value;

    public ModificarEntidadAcademicaHandlerTests()
    {
        _repositorio.Registrar(Id, _entidad);
        _repositorio.AreasAcademicasActivas.Add(3);
        _municipios.Existentes[87] = "Xalapa";
    }

    [Fact]
    public async Task HandleAsync_ConDatosValidos_ModificaYGuarda()
    {
        var resultado = await Handler().HandleAsync(Comando(), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _entidad.Nombre.ShouldBe("Facultad de Letras Españolas");
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConDatosInvalidos_NoGuarda()
    {
        var resultado = await Handler().HandleAsync(Comando(nombre: ""), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(EntidadAcademicaErrors.NombreVacio);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConAreaAcademicaInexistente_NoLlamaASaveChangesAsync()
    {
        var resultado = await Handler().HandleAsync(Comando(areaAcademicaId: 99), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(EntidadAcademicaErrors.AreaAcademicaInexistente);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConMunicipioInexistente_NoLlamaASaveChangesAsync()
    {
        var resultado = await Handler().HandleAsync(Comando(municipioId: 99), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(EntidadAcademicaErrors.MunicipioInexistente);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_CambiaElAreaAcademica_PorDecisionD6()
    {
        _repositorio.AreasAcademicasActivas.Add(5);

        var resultado = await Handler().HandleAsync(Comando(areaAcademicaId: 5), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _entidad.AreaAcademicaId.ShouldBe(5);
    }

    [Fact]
    public async Task HandleAsync_ConEntidadInexistente_FallaConNoEncontradoSinGuardar()
    {
        var resultado = await Handler().HandleAsync(
            Comando() with { Id = Id + 1 }, TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(EntidadAcademicaErrors.NoEncontrado(Id + 1));
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_CambiandoElAreaConProgramas_FallaConAreaAcademicaInmutableSinTocarLaEntidadNiGuardar()
    {
        _repositorio.AreasAcademicasActivas.Add(5);
        _programas.TieneProgramas = true;

        var resultado = await Handler().HandleAsync(
            Comando(nombre: "Otro nombre", areaAcademicaId: 5), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(EntidadAcademicaErrors.AreaAcademicaInmutable);
        _entidad.AreaAcademicaId.ShouldBe(3);
        _entidad.Nombre.ShouldBe("Facultad de Letras");
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_SinCambiarElAreaConProgramas_ModificaSinConsultarLosProgramas()
    {
        _programas.TieneProgramas = true;

        var resultado = await Handler().HandleAsync(Comando(), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _programas.ConsultasDeTodos.ShouldBe(0);
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_CambiandoElAreaSinProgramas_ModificaYGuarda()
    {
        _repositorio.AreasAcademicasActivas.Add(5);
        _programas.TieneProgramas = false;

        var resultado = await Handler().HandleAsync(Comando(areaAcademicaId: 5), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _programas.ConsultasDeTodos.ShouldBe(1);
        _entidad.AreaAcademicaId.ShouldBe(5);
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_CambiandoElAreaConProgramasSoloDadosDeBaja_FallaConAreaAcademicaInmutable()
    {
        _repositorio.AreasAcademicasActivas.Add(5);
        _programas.TieneProgramasActivos = false;
        _programas.TieneProgramas = true;

        var resultado = await Handler().HandleAsync(Comando(areaAcademicaId: 5), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(EntidadAcademicaErrors.AreaAcademicaInmutable);
        _programas.ConsultasDeActivos.ShouldBe(0);
    }

    private ModificarEntidadAcademicaHandler Handler() => new(_repositorio, _municipios, [_programas], _unidadDeTrabajo);

    private static ModificarEntidadAcademicaCommand Comando(
        string nombre = "Facultad de Letras Españolas", int areaAcademicaId = 3, int municipioId = 87) =>
        new(Id, nombre, "Calle", null, "Colonia", "91020", "2288421700", null, areaAcademicaId, municipioId);
}
