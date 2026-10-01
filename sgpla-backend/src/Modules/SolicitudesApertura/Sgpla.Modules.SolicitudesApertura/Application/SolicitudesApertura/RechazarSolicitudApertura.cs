using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.SolicitudesApertura.Application.Ambito;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;

internal sealed record RechazarSolicitudAperturaCommand(int Id, string? Comentarios);

internal sealed class RechazarSolicitudAperturaHandler(
    ISolicitudAperturaRepository repositorio,
    IAmbitoSolicitudesApertura ambito,
    ICurrentUser actual,
    IUnitOfWork unidadDeTrabajo,
    TimeProvider reloj) : ICommandHandler<RechazarSolicitudAperturaCommand>
{
    public async Task<Result> HandleAsync(RechazarSolicitudAperturaCommand command, CancellationToken cancellationToken)
    {
        var solicitud = await repositorio.ObtenerPorIdAsync(command.Id, cancellationToken);
        var experiencia = solicitud is null
            ? null
            : await ambito.ExperienciaDeSuAreaAsync(solicitud.ExperienciaEducativaId, cancellationToken);
        if (solicitud is null || experiencia is null)
        {
            return SolicitudAperturaErrors.NoEncontrada(command.Id);
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var instante = ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond));
        var rechazada = solicitud.Rechazar(command.Comentarios, actual.Id, instante);
        if (rechazada.IsFailure)
        {
            return rechazada;
        }

        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
