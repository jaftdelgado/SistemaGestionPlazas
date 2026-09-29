using Sgpla.Modules.OfertaEducativa.Application.ProgramasEducativos;
using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;
using Sgpla.UnitTests.Catalogos.Articulos;

namespace Sgpla.UnitTests.OfertaEducativa.ProgramasEducativos;

public sealed class ModificarProgramaEducativoHandlerTests
{
    private const int Id = 12;
    private const int EntidadId = 7;
    private const int SistemaId = 1;
    private const int NivelId = 3;

    private readonly ProgramaEducativoRepositoryFalso _repositorio = new();
    private readonly AmbitoOfertaEducativaFalso _ambito = new();
    private readonly ClasificacionesAcademicasFalso _clasificaciones = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();
    private readonly ProgramaEducativo _programa =
        ProgramaEducativo.Crear("Ingeniería de Software", EntidadId, SistemaId, NivelId).Value;

    public ModificarProgramaEducativoHandlerTests()
    {
        _repositorio.Registrar(Id, _programa);
        _ambito.EntidadesEscribibles.Add(EntidadId);
        _clasificaciones.Sistemas.UnionWith([SistemaId, 2]);
        _clasificaciones.Niveles.UnionWith([NivelId, 4]);
    }

    [Fact]
    public async Task HandleAsync_ConDatosValidos_ModificaYGuarda()
    {
        var resultado = await Handler().HandleAsync(Comando(nombre: " Software  Libre", sistemaId: 2, nivelId: 4), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _programa.Nombre.ShouldBe("Software Libre");
        _programa.SistemaEducativoId.ShouldBe(2);
        _programa.NivelFormacionId.ShouldBe(4);
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConProgramaInexistente_FallaConNoEncontradoSinGuardar()
    {
        var resultado = await Handler().HandleAsync(Comando() with { Id = Id + 1 }, TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ProgramaEducativoErrors.NoEncontrado(Id + 1));
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConProgramaDeOtraArea_FallaConNoEncontradoSinGuardar()
    {
        _ambito.EntidadesEscribibles.Clear();

        var resultado = await Handler().HandleAsync(Comando(nombre: "Otro nombre"), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ProgramaEducativoErrors.NoEncontrado(Id));
        _programa.Nombre.ShouldBe("Ingeniería de Software");
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConNombreInvalido_FallaSinGuardar()
    {
        var resultado = await Handler().HandleAsync(Comando(nombre: ""), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ProgramaEducativoErrors.NombreVacio);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConPlanesYCambioDeSistema_FallaConClasificacionInmutableSinGuardar()
    {
        _repositorio.TuvoPlanes = true;

        var resultado = await Handler().HandleAsync(Comando(sistemaId: 2), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ProgramaEducativoErrors.ClasificacionInmutable);
        _programa.SistemaEducativoId.ShouldBe(SistemaId);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConPlanesYSoloElNombre_ModificaYGuarda()
    {
        _repositorio.TuvoPlanes = true;

        var resultado = await Handler().HandleAsync(Comando(nombre: "Otro nombre"), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _programa.Nombre.ShouldBe("Otro nombre");
        _unidadDeTrabajo.Guardados.ShouldBe(1);
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
    public async Task HandleAsync_ConNombreDeOtroProgramaDeLaEntidadYSistema_FallaConNombreDuplicadoSinGuardar()
    {
        _repositorio.RegistrarNombre(EntidadId, "Software Libre", SistemaId, id: Id + 1);

        var resultado = await Handler().HandleAsync(Comando(nombre: "Software Libre"), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ProgramaEducativoErrors.NombreDuplicado);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConSuPropioNombre_NoLoTomaPorDuplicado()
    {
        var resultado = await Handler().HandleAsync(Comando(nombre: "Ingeniería de Software"), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    private ModificarProgramaEducativoHandler Handler() => new(_repositorio, _ambito, _clasificaciones, _unidadDeTrabajo);

    private static ModificarProgramaEducativoCommand Comando(
        string nombre = "Ingeniería de Software", int sistemaId = SistemaId, int nivelId = NivelId) =>
        new(Id, nombre, sistemaId, nivelId);
}
