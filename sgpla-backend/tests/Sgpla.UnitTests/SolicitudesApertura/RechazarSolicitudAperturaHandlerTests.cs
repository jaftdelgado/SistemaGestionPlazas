using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;
using Sgpla.SharedKernel;

namespace Sgpla.UnitTests.SolicitudesApertura;

public sealed class RechazarSolicitudAperturaHandlerTests
{
    private const int AreaDgaa = DatosDePrueba.AreaId;

    private readonly SolicitudAperturaRepositoryFalso _repositorio = new();
    private readonly ExperienciasEducativasFalsas _experiencias = new();
    private readonly UsuarioActualFalso _actual = new(88, Rol.Dgaa, null, AreaDgaa);
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();
    private readonly RelojFalso _reloj = new(new DateTimeOffset(2026, 10, 2, 9, 30, 0, 987, TimeSpan.Zero));
    private readonly SolicitudApertura _solicitud = DatosDePrueba.Solicitud();

    public RechazarSolicitudAperturaHandlerTests()
    {
        _experiencias.Resumenes.Add(DatosDePrueba.ExperienciaId, DatosDePrueba.Experiencia());
        _repositorio.Precargar(DatosDePrueba.SolicitudId, _solicitud);
    }

    [Fact]
    public async Task HandleAsync_ConIdInexistente_DevuelveNoEncontrada()
    {
        var resultado = await Handler().HandleAsync(
            new RechazarSolicitudAperturaCommand(999, "No procede."), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.NoEncontrada(999));
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConSolicitudDeOtraArea_DevuelveNoEncontrada()
    {
        _experiencias.Resumenes[DatosDePrueba.ExperienciaId] = DatosDePrueba.Experiencia(areaId: AreaDgaa + 1);

        var resultado = await Handler().HandleAsync(Comando("No procede."), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.NoEncontrada(DatosDePrueba.SolicitudId));
        _solicitud.Estado.ShouldBe(EstadoSolicitudApertura.Pendiente);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConComentarios_RechazaConActorYFecha()
    {
        var resultado = await Handler().HandleAsync(Comando("  No hay profesor disponible.  "), CancellationToken.None);

        resultado.IsSuccess.ShouldBeTrue();
        _solicitud.Estado.ShouldBe(EstadoSolicitudApertura.Rechazada);
        _solicitud.ComentariosResolucion.ShouldBe("No hay profesor disponible.");
        _solicitud.ResueltaPorUsuarioId.ShouldBe(88);
        _solicitud.ResueltaEn.ShouldBe(new DateTime(2026, 10, 2, 9, 30, 0, DateTimeKind.Utc));
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task HandleAsync_SinComentarios_FallaEnComentarios(string? comentarios)
    {
        var resultado = await Handler().HandleAsync(Comando(comentarios), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.ComentariosVacios);
        _solicitud.Estado.ShouldBe(EstadoSolicitudApertura.Pendiente);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConSolicitudNoPendiente_DevuelveNoPendiente()
    {
        _solicitud.Aceptar(null, 10, 40, 88, DatosDePrueba.Fecha).IsSuccess.ShouldBeTrue();

        var resultado = await Handler().HandleAsync(Comando("No procede."), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.NoPendiente);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    private static RechazarSolicitudAperturaCommand Comando(string? comentarios) =>
        new(DatosDePrueba.SolicitudId, comentarios);

    private RechazarSolicitudAperturaHandler Handler() => new(
        _repositorio,
        _experiencias,
        _actual,
        _unidadDeTrabajo,
        _reloj);
}
