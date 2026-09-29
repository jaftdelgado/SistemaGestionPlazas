using Sgpla.Modules.OfertaEducativa.Application.ProgramasEducativos;
using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;
using Sgpla.UnitTests.Catalogos.Articulos;

namespace Sgpla.UnitTests.OfertaEducativa.ProgramasEducativos;

public sealed class CrearProgramaEducativoHandlerTests
{
    private const int EntidadId = 7;
    private const int SistemaId = 1;
    private const int NivelId = 3;

    private readonly ProgramaEducativoRepositoryFalso _repositorio = new();
    private readonly AmbitoOfertaEducativaFalso _ambito = new();
    private readonly ClasificacionesAcademicasFalso _clasificaciones = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();

    public CrearProgramaEducativoHandlerTests()
    {
        _ambito.EntidadesEscribibles.Add(EntidadId);
        _clasificaciones.Sistemas.Add(SistemaId);
        _clasificaciones.Niveles.Add(NivelId);
    }

    [Fact]
    public async Task HandleAsync_ConDatosValidos_AgregaYGuarda()
    {
        var resultado = await Handler().HandleAsync(Comando(nombre: "  Ingeniería   de Software "), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        var agregado = _repositorio.Agregados.ShouldHaveSingleItem();
        agregado.Nombre.ShouldBe("Ingeniería de Software");
        agregado.EntidadAcademicaId.ShouldBe(EntidadId);
        agregado.SistemaEducativoId.ShouldBe(SistemaId);
        agregado.NivelFormacionId.ShouldBe(NivelId);
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConNombreInvalido_FallaSinGuardar()
    {
        var resultado = await Handler().HandleAsync(Comando(nombre: "  "), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ProgramaEducativoErrors.NombreVacio);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConEntidadFueraDelAmbito_FallaConEntidadAcademicaInexistenteSinGuardar()
    {
        var resultado = await Handler().HandleAsync(Comando(entidadId: EntidadId + 1), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ProgramaEducativoErrors.EntidadAcademicaInexistente);
        _repositorio.Agregados.ShouldBeEmpty();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConSistemaInexistente_FallaConSistemaEducativoInexistenteSinGuardar()
    {
        var resultado = await Handler().HandleAsync(Comando(sistemaId: 99), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ProgramaEducativoErrors.SistemaEducativoInexistente);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConNivelInexistente_FallaConNivelFormacionInexistenteSinGuardar()
    {
        var resultado = await Handler().HandleAsync(Comando(nivelId: 99), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ProgramaEducativoErrors.NivelFormacionInexistente);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConEntidadYSistemaInvalidos_DevuelveElErrorDeLaEntidad()
    {
        var resultado = await Handler().HandleAsync(
            Comando(entidadId: EntidadId + 1, sistemaId: 99), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ProgramaEducativoErrors.EntidadAcademicaInexistente);
    }

    [Fact]
    public async Task HandleAsync_ConNombreExistenteEnLaMismaEntidadYSistema_FallaConNombreDuplicadoSinGuardar()
    {
        _repositorio.RegistrarNombre(EntidadId, "Ingeniería de Software", SistemaId, id: 1);

        var resultado = await Handler().HandleAsync(Comando(nombre: " Ingeniería  de Software"), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ProgramaEducativoErrors.NombreDuplicado);
        _repositorio.Agregados.ShouldBeEmpty();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConElMismoNombreEnOtroSistema_Guarda()
    {
        _clasificaciones.Sistemas.Add(2);
        _repositorio.RegistrarNombre(EntidadId, "Ingeniería de Software", 2, id: 1);

        var resultado = await Handler().HandleAsync(Comando(nombre: "Ingeniería de Software"), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    private CrearProgramaEducativoHandler Handler() => new(_repositorio, _ambito, _clasificaciones, _unidadDeTrabajo);

    private static CrearProgramaEducativoCommand Comando(
        string nombre = "Ingeniería de Software", int entidadId = EntidadId, int sistemaId = SistemaId, int nivelId = NivelId) =>
        new(nombre, entidadId, sistemaId, nivelId);
}
