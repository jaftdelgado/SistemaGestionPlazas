using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;
using Sgpla.SharedKernel;

namespace Sgpla.UnitTests.SolicitudesApertura;

public sealed class CancelarSolicitudAperturaHandlerTests
{
    private readonly SolicitudAperturaRepositoryFalso _repositorio = new();
    private readonly AmbitoSolicitudesAperturaFalso _ambito = new();
    private readonly UsuarioActualFalso _actual = new(44, Rol.EntidadAcademica, DatosDePrueba.EntidadId);
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();
    private readonly RelojFalso _reloj = new(new DateTimeOffset(2026, 10, 2, 9, 30, 0, 987, TimeSpan.Zero));
    private readonly SolicitudApertura _solicitud = DatosDePrueba.Solicitud();

    public CancelarSolicitudAperturaHandlerTests()
    {
        _ambito.DeSuEntidad.Add(DatosDePrueba.ExperienciaId, DatosDePrueba.Experiencia());
        _repositorio.Precargar(DatosDePrueba.SolicitudId, _solicitud);
    }

    [Fact]
    public async Task HandleAsync_ConIdInexistente_DevuelveNoEncontrada()
    {
        var resultado = await Handler().HandleAsync(
            new CancelarSolicitudAperturaCommand(999, "Ya no se necesita."), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.NoEncontrada(999));
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConSolicitudFueraDelAmbito_DevuelveNoEncontrada()
    {
        _ambito.DeSuEntidad.Remove(DatosDePrueba.ExperienciaId);

        var resultado = await Handler().HandleAsync(Comando("Ya no se necesita."), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.NoEncontrada(DatosDePrueba.SolicitudId));
        _solicitud.Estado.ShouldBe(EstadoSolicitudApertura.Pendiente);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConMotivo_CancelaConActorYFecha()
    {
        var resultado = await Handler().HandleAsync(Comando("  Ya no se necesita.  "), CancellationToken.None);

        resultado.IsSuccess.ShouldBeTrue();
        _solicitud.Estado.ShouldBe(EstadoSolicitudApertura.Cancelada);
        _solicitud.MotivoCancelacion.ShouldBe("Ya no se necesita.");
        _solicitud.CanceladaPorUsuarioId.ShouldBe(44);
        _solicitud.CanceladaEn.ShouldBe(new DateTime(2026, 10, 2, 9, 30, 0, DateTimeKind.Utc));
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task HandleAsync_SinMotivo_FallaEnMotivo(string? motivo)
    {
        var resultado = await Handler().HandleAsync(Comando(motivo), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.MotivoVacio);
        _solicitud.Estado.ShouldBe(EstadoSolicitudApertura.Pendiente);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConSolicitudNoPendiente_DevuelveNoPendiente()
    {
        _solicitud.Rechazar("No procede.", 88, DatosDePrueba.Fecha).IsSuccess.ShouldBeTrue();

        var resultado = await Handler().HandleAsync(Comando("Ya no se necesita."), CancellationToken.None);

        resultado.Error.ShouldBe(SolicitudAperturaErrors.NoPendiente);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    private static CancelarSolicitudAperturaCommand Comando(string? motivo) => new(DatosDePrueba.SolicitudId, motivo);

    private CancelarSolicitudAperturaHandler Handler() => new(
        _repositorio,
        _ambito,
        _actual,
        _unidadDeTrabajo,
        _reloj);
}
