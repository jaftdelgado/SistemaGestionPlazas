using Sgpla.Modules.OfertaEducativa.Application.ExperienciasEducativas;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;
using Sgpla.UnitTests.Catalogos.Articulos;

namespace Sgpla.UnitTests.OfertaEducativa.ExperienciasEducativas;

public sealed class ModificarExperienciaEducativaHandlerTests
{
    private const int Id = 8;
    private const int EntidadId = 7;

    private readonly ExperienciaEducativaRepositoryFalso _repositorio = new();
    private readonly AmbitoOfertaEducativaFalso _ambito = new();
    private readonly ClasificacionesAcademicasFalso _clasificaciones = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();
    private readonly ExperienciaEducativa _experiencia = ExperienciaEducativa.Crear(
        new DatosExperienciaEducativa("Habilidades", "ENSO", "38003", 2, 3, 6, 5, 30, "Perfil", 1)).Value;

    public ModificarExperienciaEducativaHandlerTests()
    {
        _repositorio.Registrar(Id, _experiencia);
        _repositorio.EntidadDeLaExperiencia = EntidadId;
        _ambito.EntidadesEscribibles.Add(EntidadId);
        _clasificaciones.Areas.UnionWith([1, 2, 3]);
    }

    [Fact]
    public async Task HandleAsync_SinProgramaciones_CambiaTodoLoEditableYGuardaUnaVez()
    {
        var resultado = await Handler().HandleAsync(
            Comando() with { Nombre = "  Otro   nombre ", HorasTeoricas = 4, HorasPracticas = 5, Creditos = 9, AreaFormacionId = 2 },
            TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _experiencia.Nombre.ShouldBe("Otro nombre");
        _experiencia.HorasTeoricas.ShouldBe(4);
        _experiencia.HorasPracticas.ShouldBe(5);
        _experiencia.Creditos.ShouldBe(9);
        _experiencia.AreaFormacionId.ShouldBe(2);
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConProgramacionesYSoloNombrePerfilYCupos_LosCambiaYGuarda()
    {
        _repositorio.TuvoProgramaciones = true;

        var resultado = await Handler().HandleAsync(
            Comando() with { Nombre = "Otro nombre", CupoMinimo = 10, CupoMaximo = 40, PerfilDocente = "Otro perfil" },
            TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _experiencia.Nombre.ShouldBe("Otro nombre");
        _experiencia.CupoMinimo.ShouldBe(10);
        _experiencia.CupoMaximo.ShouldBe(40);
        _experiencia.PerfilDocente.ShouldBe("Otro perfil");
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConProgramacionesYCambioDeHoras_FallaConAtributosCurricularesInmutablesSinGuardar()
    {
        _repositorio.TuvoProgramaciones = true;

        var resultado = await Handler().HandleAsync(
            Comando() with { HorasTeoricas = 4 }, TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ExperienciaEducativaErrors.AtributosCurricularesInmutables);
        _experiencia.HorasTeoricas.ShouldBe(2);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConExperienciaInexistente_FallaConNoEncontradoSinGuardar()
    {
        var resultado = await Handler().HandleAsync(
            Comando() with { Id = Id + 1 }, TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ExperienciaEducativaErrors.NoEncontrado(Id + 1));
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConExperienciaDeOtraArea_FallaConNoEncontradoSinGuardar()
    {
        _ambito.EntidadesEscribibles.Clear();

        var resultado = await Handler().HandleAsync(Comando() with { Nombre = "Otro" }, TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ExperienciaEducativaErrors.NoEncontrado(Id));
        _experiencia.Nombre.ShouldBe("Habilidades");
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConCuposInvertidos_FallaConCuposInvertidosSinGuardar()
    {
        var resultado = await Handler().HandleAsync(
            Comando() with { CupoMinimo = 40, CupoMaximo = 10 }, TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ExperienciaEducativaErrors.CuposInvertidos);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConAreaInexistente_FallaConAreaFormacionInexistenteSinGuardar()
    {
        var resultado = await Handler().HandleAsync(
            Comando() with { AreaFormacionId = 99 }, TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(ExperienciaEducativaErrors.AreaFormacionInexistente);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConCuposYPerfilNull_LosQuita()
    {
        var resultado = await Handler().HandleAsync(
            Comando() with { CupoMinimo = null, CupoMaximo = null, PerfilDocente = null }, TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _experiencia.CupoMinimo.ShouldBeNull();
        _experiencia.CupoMaximo.ShouldBeNull();
        _experiencia.PerfilDocente.ShouldBeNull();
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    private static ModificarExperienciaEducativaCommand Comando() =>
        new(Id, "Habilidades", 2, 3, 6, 5, 30, "Perfil", 1);

    private ModificarExperienciaEducativaHandler Handler() => new(_repositorio, _ambito, _clasificaciones, _unidadDeTrabajo);
}
