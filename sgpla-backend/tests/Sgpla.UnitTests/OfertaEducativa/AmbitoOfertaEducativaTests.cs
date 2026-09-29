using Sgpla.Modules.OfertaEducativa.Infrastructure.Ambito;
using Sgpla.SharedKernel;

namespace Sgpla.UnitTests.OfertaEducativa;

public sealed class AmbitoOfertaEducativaTests
{
    private const int Area = 3;
    private const int OtraArea = 4;
    private const int EntidadActiva = 10;
    private const int EntidadDadaDeBaja = 11;
    private const int EntidadDeOtraArea = 20;

    private readonly AmbitosDeEntidadesFalso _ambitos = new();

    public AmbitoOfertaEducativaTests()
    {
        _ambitos.Registrar(EntidadActiva, Area);
        _ambitos.Registrar(EntidadDadaDeBaja, Area, activa: false);
        _ambitos.Registrar(EntidadDeOtraArea, OtraArea);
    }

    [Fact]
    public async Task EntidadesVisiblesAsync_Superusuario_DevuelveNullQueSignificaTodas()
    {
        var entidades = await Ambito(Rol.Superusuario).EntidadesVisiblesAsync(TestContext.Current.CancellationToken);

        entidades.ShouldBeNull();
    }

    [Fact]
    public async Task EntidadesVisiblesAsync_Dgaa_DevuelveLasEntidadesDeSuAreaIncluidasLasDadasDeBaja()
    {
        var entidades = await Ambito(Rol.Dgaa, areaAcademicaId: Area)
            .EntidadesVisiblesAsync(TestContext.Current.CancellationToken);

        entidades.ShouldNotBeNull().ShouldBe([EntidadActiva, EntidadDadaDeBaja], ignoreOrder: true);
    }

    [Fact]
    public async Task EntidadesVisiblesAsync_EntidadAcademica_DevuelveSoloSuEntidad()
    {
        var entidades = await Ambito(Rol.EntidadAcademica, entidadAcademicaId: EntidadDeOtraArea)
            .EntidadesVisiblesAsync(TestContext.Current.CancellationToken);

        entidades.ShouldNotBeNull().ShouldBe([EntidadDeOtraArea]);
    }

    [Fact]
    public async Task EntidadesVisiblesAsync_RolDesconocido_DevuelveColeccionVacia()
    {
        var entidades = await Ambito((Rol)99, areaAcademicaId: Area, entidadAcademicaId: EntidadActiva)
            .EntidadesVisiblesAsync(TestContext.Current.CancellationToken);

        entidades.ShouldNotBeNull().ShouldBeEmpty();
    }

    [Fact]
    public async Task EntidadesVisiblesAsync_DgaaSinArea_DevuelveColeccionVacia()
    {
        var entidades = await Ambito(Rol.Dgaa).EntidadesVisiblesAsync(TestContext.Current.CancellationToken);

        entidades.ShouldNotBeNull().ShouldBeEmpty();
    }

    [Fact]
    public async Task EntidadesVisiblesAsync_EntidadAcademicaSinEntidad_DevuelveColeccionVacia()
    {
        var entidades = await Ambito(Rol.EntidadAcademica).EntidadesVisiblesAsync(TestContext.Current.CancellationToken);

        entidades.ShouldNotBeNull().ShouldBeEmpty();
    }

    [Fact]
    public async Task PuedeEscribirEnEntidadAsync_DgaaEnEntidadActivaDeSuArea_DevuelveTrue()
    {
        var puede = await Ambito(Rol.Dgaa, areaAcademicaId: Area)
            .PuedeEscribirEnEntidadAsync(EntidadActiva, TestContext.Current.CancellationToken);

        puede.ShouldBeTrue();
    }

    [Fact]
    public async Task PuedeEscribirEnEntidadAsync_DgaaEnEntidadDeOtraArea_DevuelveFalse()
    {
        var puede = await Ambito(Rol.Dgaa, areaAcademicaId: Area)
            .PuedeEscribirEnEntidadAsync(EntidadDeOtraArea, TestContext.Current.CancellationToken);

        puede.ShouldBeFalse();
    }

    [Fact]
    public async Task PuedeEscribirEnEntidadAsync_DgaaEnEntidadDadaDeBaja_DevuelveFalse()
    {
        var puede = await Ambito(Rol.Dgaa, areaAcademicaId: Area)
            .PuedeEscribirEnEntidadAsync(EntidadDadaDeBaja, TestContext.Current.CancellationToken);

        puede.ShouldBeFalse();
    }

    [Fact]
    public async Task PuedeEscribirEnEntidadAsync_DgaaEnEntidadInexistente_DevuelveFalse()
    {
        var puede = await Ambito(Rol.Dgaa, areaAcademicaId: Area)
            .PuedeEscribirEnEntidadAsync(int.MaxValue, TestContext.Current.CancellationToken);

        puede.ShouldBeFalse();
    }

    [Fact]
    public async Task PuedeEscribirEnEntidadAsync_DgaaSinArea_DevuelveFalse()
    {
        var puede = await Ambito(Rol.Dgaa).PuedeEscribirEnEntidadAsync(EntidadActiva, TestContext.Current.CancellationToken);

        puede.ShouldBeFalse();
    }

    [Theory]
    [InlineData(Rol.Superusuario)]
    [InlineData(Rol.EntidadAcademica)]
    [InlineData((Rol)99)]
    public async Task PuedeEscribirEnEntidadAsync_ConCualquierOtroRol_DevuelveFalse(Rol rol)
    {
        var puede = await Ambito(rol, areaAcademicaId: Area, entidadAcademicaId: EntidadActiva)
            .PuedeEscribirEnEntidadAsync(EntidadActiva, TestContext.Current.CancellationToken);

        puede.ShouldBeFalse();
    }

    private AmbitoOfertaEducativa Ambito(Rol rol, int? areaAcademicaId = null, int? entidadAcademicaId = null) =>
        new(new UsuarioActualFalso(rol, areaAcademicaId, entidadAcademicaId), _ambitos);
}
