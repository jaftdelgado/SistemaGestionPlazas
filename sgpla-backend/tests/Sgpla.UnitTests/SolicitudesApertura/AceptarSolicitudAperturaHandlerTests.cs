using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;
using Sgpla.SharedKernel;

namespace Sgpla.UnitTests.SolicitudesApertura;

public sealed class AceptarSolicitudAperturaHandlerTests
{
    private const int AreaDgaa = DatosDePrueba.AreaId;

    private readonly SolicitudAperturaRepositoryFalso _repositorio = new();
    private readonly ExperienciasEducativasFalsas _experiencias = new();
    private readonly UsuarioActualFalso _actual = new(88, Rol.Dgaa, null, AreaDgaa);
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();
    private readonly RelojFalso _reloj = new(new DateTimeOffset(2026, 10, 2, 9, 30, 0, 987, TimeSpan.Zero));
    private readonly SolicitudApertura _solicitud = DatosDePrueba.Solicitud();

    public AceptarSolicitudAperturaHandlerTests()
    {
        _experiencias.Resumenes.Add(DatosDePrueba.ExperienciaId, DatosDePrueba.Experiencia());
        _repositorio.Precargar(DatosDePrueba.SolicitudId, _solicitud);
    }

    [Fact]
    public async Task HandleAsync_ConIdInexistente_DevuelveNoEncontrada()
    {
        var resultado = await Handler().HandleAsync(new AceptarSolicitudAperturaCommand(999, null), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.NoEncontrada(999));
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConSolicitudDeOtraArea_DevuelveNoEncontrada()
    {
        _experiencias.Resumenes[DatosDePrueba.ExperienciaId] = DatosDePrueba.Experiencia(areaId: AreaDgaa + 1);

        var resultado = await Handler().HandleAsync(Comando(), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.NoEncontrada(DatosDePrueba.SolicitudId));
        _solicitud.Estado.ShouldBe(EstadoSolicitudApertura.Pendiente);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConComentarios_AceptaConActorYFecha()
    {
        var resultado = await Handler().HandleAsync(
            new AceptarSolicitudAperturaCommand(DatosDePrueba.SolicitudId, "  Visto bueno.  "), CancellationToken.None);

        resultado.IsSuccess.ShouldBeTrue();
        _solicitud.Estado.ShouldBe(EstadoSolicitudApertura.Aceptada);
        _solicitud.ComentariosResolucion.ShouldBe("Visto bueno.");
        _solicitud.ResueltaPorUsuarioId.ShouldBe(88);
        _solicitud.ResueltaEn.ShouldBe(new DateTime(2026, 10, 2, 9, 30, 0, DateTimeKind.Utc));
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_SinComentarios_AceptaConComentariosNulos()
    {
        var resultado = await Handler().HandleAsync(Comando(), CancellationToken.None);

        resultado.IsSuccess.ShouldBeTrue();
        _solicitud.ComentariosResolucion.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_UsaLosCuposVigentesDeLaExperiencia()
    {
        _experiencias.Resumenes[DatosDePrueba.ExperienciaId] = DatosDePrueba.Experiencia(cupoMinimo: 10, cupoMaximo: 15);

        var resultado = await Handler().HandleAsync(Comando(), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.CantidadFueraDeCupos);
        _solicitud.Estado.ShouldBe(EstadoSolicitudApertura.Pendiente);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Theory]
    [InlineData(null, 40)]
    [InlineData(10, null)]
    public async Task HandleAsync_ConCuposIncompletos_DevuelveCuposIncompletos(int? minimo, int? maximo)
    {
        _experiencias.Resumenes[DatosDePrueba.ExperienciaId] = DatosDePrueba.Experiencia(minimo, maximo);

        var resultado = await Handler().HandleAsync(Comando(), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.CuposIncompletos);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConSolicitudNoPendiente_DevuelveNoPendiente()
    {
        _solicitud.Rechazar("No procede.", 88, DatosDePrueba.Fecha).IsSuccess.ShouldBeTrue();

        var resultado = await Handler().HandleAsync(Comando(), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.NoPendiente);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConComentariosDemasiadoLargos_FallaEnComentarios()
    {
        var resultado = await Handler().HandleAsync(
            new AceptarSolicitudAperturaCommand(DatosDePrueba.SolicitudId, new string('x', 2001)), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.ComentariosDemasiadoLargos);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    private static AceptarSolicitudAperturaCommand Comando() => new(DatosDePrueba.SolicitudId, null);

    private AceptarSolicitudAperturaHandler Handler() => new(
        _repositorio,
        _experiencias,
        _actual,
        _unidadDeTrabajo,
        _reloj);
}
