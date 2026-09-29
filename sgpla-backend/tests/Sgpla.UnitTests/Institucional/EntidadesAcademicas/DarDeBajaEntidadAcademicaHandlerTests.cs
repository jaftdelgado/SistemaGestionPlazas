using Sgpla.Modules.Institucional.Application.EntidadesAcademicas;
using Sgpla.Modules.Institucional.Domain.EntidadesAcademicas;
using Sgpla.UnitTests.Catalogos.Articulos;

namespace Sgpla.UnitTests.Institucional.EntidadesAcademicas;

public sealed class DarDeBajaEntidadAcademicaHandlerTests
{
    private const int Id = 7;

    private readonly EntidadAcademicaRepositoryFalso _repositorio = new();
    private readonly UsuariosDeEntidadAcademicaFalso _usuarios = new();
    private readonly ProgramasDeEntidadAcademicaFalso _programas = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();
    private readonly EntidadAcademica _entidad = EntidadAcademica.Crear(
        "FLX1", "Facultad de Letras", "Calle", null, "Colonia", "91020", "2288421700", null, 1, 3, 87).Value;

    public DarDeBajaEntidadAcademicaHandlerTests() => _repositorio.Registrar(Id, _entidad);

    [Fact]
    public async Task HandleAsync_Activa_DaDeBajaYGuarda()
    {
        var ahoraConMilisegundos = new DateTimeOffset(2026, 3, 15, 10, 30, 45, 250, TimeSpan.Zero);

        var resultado = await Handler(ahoraConMilisegundos).HandleAsync(
            new DarDeBajaEntidadAcademicaCommand(Id), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _entidad.FechaEliminacion.ShouldBe(new DateTime(2026, 3, 15, 10, 30, 45, DateTimeKind.Utc));
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConEntidadInexistente_FallaConNoEncontradoSinGuardar()
    {
        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaEntidadAcademicaCommand(Id + 1), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(EntidadAcademicaErrors.NoEncontrado(Id + 1));
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConUsuariosActivos_FallaConTieneUsuariosActivosSinGuardar()
    {
        _usuarios.TieneUsuariosActivos = true;

        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaEntidadAcademicaCommand(Id), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(EntidadAcademicaErrors.TieneUsuariosActivos);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConProgramasActivos_FallaConTieneProgramasActivosSinGuardar()
    {
        _programas.TieneProgramasActivos = true;

        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaEntidadAcademicaCommand(Id), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(EntidadAcademicaErrors.TieneProgramasActivos);
        _entidad.FechaEliminacion.ShouldBeNull();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConProgramasSoloDadosDeBaja_DaDeBajaConsultandoSoloLosActivos()
    {
        _programas.TieneProgramasActivos = false;
        _programas.TieneProgramas = true;

        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaEntidadAcademicaCommand(Id), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _programas.ConsultasDeActivos.ShouldBe(1);
        _programas.ConsultasDeTodos.ShouldBe(0);
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConUsuariosYProgramasActivos_DevuelveElErrorDeLosUsuariosSinConsultarLosProgramas()
    {
        _usuarios.TieneUsuariosActivos = true;
        _programas.TieneProgramasActivos = true;

        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaEntidadAcademicaCommand(Id), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(EntidadAcademicaErrors.TieneUsuariosActivos);
        _programas.ConsultasDeActivos.ShouldBe(0);
    }

    private DarDeBajaEntidadAcademicaHandler Handler(DateTimeOffset ahora) =>
        new(_repositorio, [_usuarios], [_programas], _unidadDeTrabajo, new TimeProviderFalso(ahora));
}
