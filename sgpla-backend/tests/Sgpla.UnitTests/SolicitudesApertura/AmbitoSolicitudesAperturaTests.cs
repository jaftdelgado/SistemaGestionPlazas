using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.SolicitudesApertura.Infrastructure.Ambito;
using Sgpla.SharedKernel;

namespace Sgpla.UnitTests.SolicitudesApertura;

public sealed class AmbitoSolicitudesAperturaTests
{
    private const int Entidad = 7;
    private const int OtraEntidad = 8;
    private const int Area = 3;
    private const int OtraArea = 4;
    private const int ExperienciaPropia = 301;
    private const int ExperienciaAjena = 302;

    private readonly ExperienciasEducativasFalsas _experiencias = new();

    public AmbitoSolicitudesAperturaTests()
    {
        _experiencias.Resumenes.Add(ExperienciaPropia, Experiencia(ExperienciaPropia, Entidad, Area));
        _experiencias.Resumenes.Add(ExperienciaAjena, Experiencia(ExperienciaAjena, OtraEntidad, OtraArea));
    }

    [Fact]
    public async Task ExperienciaDeSuEntidadAsync_EntidadAcademicaConEeDeSuEntidad_DevuelveElResumen()
    {
        var resumen = await Ambito(Rol.EntidadAcademica, entidadAcademicaId: Entidad)
            .ExperienciaDeSuEntidadAsync(ExperienciaPropia, TestContext.Current.CancellationToken);

        resumen.ShouldNotBeNull().Id.ShouldBe(ExperienciaPropia);
    }

    [Fact]
    public async Task ExperienciaDeSuEntidadAsync_EntidadAcademicaConEeDeOtraEntidad_DevuelveNull()
    {
        var resumen = await Ambito(Rol.EntidadAcademica, entidadAcademicaId: Entidad)
            .ExperienciaDeSuEntidadAsync(ExperienciaAjena, TestContext.Current.CancellationToken);

        resumen.ShouldBeNull();
    }

    [Fact]
    public async Task ExperienciaDeSuAreaAsync_DgaaConEeDeSuArea_DevuelveElResumen()
    {
        var resumen = await Ambito(Rol.Dgaa, areaAcademicaId: Area)
            .ExperienciaDeSuAreaAsync(ExperienciaPropia, TestContext.Current.CancellationToken);

        resumen.ShouldNotBeNull().Id.ShouldBe(ExperienciaPropia);
    }

    [Fact]
    public async Task ExperienciaDeSuAreaAsync_DgaaConEeDeOtraArea_DevuelveNull()
    {
        var resumen = await Ambito(Rol.Dgaa, areaAcademicaId: Area)
            .ExperienciaDeSuAreaAsync(ExperienciaAjena, TestContext.Current.CancellationToken);

        resumen.ShouldBeNull();
    }

    [Fact]
    public async Task ExperienciaDeSuEntidadAsync_ConDgaaOSuperusuario_DevuelveNullSinConsultar()
    {
        var dgaa = await Ambito(Rol.Dgaa, areaAcademicaId: Area)
            .ExperienciaDeSuEntidadAsync(ExperienciaPropia, TestContext.Current.CancellationToken);
        var superusuario = await Ambito(Rol.Superusuario)
            .ExperienciaDeSuEntidadAsync(ExperienciaPropia, TestContext.Current.CancellationToken);

        dgaa.ShouldBeNull();
        superusuario.ShouldBeNull();
        _experiencias.Consultas.ShouldBe(0);
    }

    [Fact]
    public async Task ExperienciaDeSuAreaAsync_ConSuperusuario_DevuelveNullSinConsultar()
    {
        var resumen = await Ambito(Rol.Superusuario)
            .ExperienciaDeSuAreaAsync(ExperienciaPropia, TestContext.Current.CancellationToken);

        resumen.ShouldBeNull();
        _experiencias.Consultas.ShouldBe(0);
    }

    [Fact]
    public async Task ExperienciaDeSuAreaAsync_ConEntidadAcademica_DevuelveNullSinConsultar()
    {
        var resumen = await Ambito(Rol.EntidadAcademica, entidadAcademicaId: Entidad)
            .ExperienciaDeSuAreaAsync(ExperienciaPropia, TestContext.Current.CancellationToken);

        resumen.ShouldBeNull();
        _experiencias.Consultas.ShouldBe(0);
    }

    [Fact]
    public async Task ExperienciaDeSuEntidadAsync_ConCuentaSinEntidad_DevuelveNullSinConsultar()
    {
        var resumen = await Ambito(Rol.EntidadAcademica)
            .ExperienciaDeSuEntidadAsync(ExperienciaPropia, TestContext.Current.CancellationToken);

        resumen.ShouldBeNull();
        _experiencias.Consultas.ShouldBe(0);
    }

    [Fact]
    public async Task ExperienciaDeSuAreaAsync_ConCuentaSinArea_DevuelveNullSinConsultar()
    {
        var resumen = await Ambito(Rol.Dgaa)
            .ExperienciaDeSuAreaAsync(ExperienciaPropia, TestContext.Current.CancellationToken);

        resumen.ShouldBeNull();
        _experiencias.Consultas.ShouldBe(0);
    }

    [Fact]
    public async Task ConExperienciaInexistente_AmbosMetodosDevuelvenNull()
    {
        var deEntidad = await Ambito(Rol.EntidadAcademica, entidadAcademicaId: Entidad)
            .ExperienciaDeSuEntidadAsync(999, TestContext.Current.CancellationToken);
        var deArea = await Ambito(Rol.Dgaa, areaAcademicaId: Area)
            .ExperienciaDeSuAreaAsync(999, TestContext.Current.CancellationToken);

        deEntidad.ShouldBeNull();
        deArea.ShouldBeNull();
    }

    private static ExperienciaEducativaResumen Experiencia(
        int id,
        int entidadId,
        int areaId) =>
        DatosDePrueba.Experiencia(entidadId: entidadId, areaId: areaId) with { Id = id };

    private AmbitoSolicitudesApertura Ambito(Rol rol, int? entidadAcademicaId = null, int? areaAcademicaId = null) =>
        new(new UsuarioActualFalso(44, rol, entidadAcademicaId, areaAcademicaId), _experiencias);
}
