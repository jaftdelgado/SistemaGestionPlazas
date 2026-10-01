using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.SolicitudesApertura.Application.Ambito;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;

internal sealed record CancelarSolicitudAperturaCommand(int Id, string? Motivo);

internal sealed class CancelarSolicitudAperturaHandler(
    ISolicitudAperturaRepository repositorio,
    IAmbitoSolicitudesApertura ambito,
    ICurrentUser actual,
    IUnitOfWork unidadDeTrabajo,
    TimeProvider reloj) : ICommandHandler<CancelarSolicitudAperturaCommand>
{
    public async Task<Result> HandleAsync(CancelarSolicitudAperturaCommand command, CancellationToken cancellationToken)
    {
        var solicitud = await repositorio.ObtenerPorIdAsync(command.Id, cancellationToken);
        var experiencia = solicitud is null
            ? null
            : await ambito.ExperienciaDeSuEntidadAsync(solicitud.ExperienciaEducativaId, cancellationToken);
        if (solicitud is null || experiencia is null)
        {
            return SolicitudAperturaErrors.NoEncontrada(command.Id);
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var instante = ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond));
        var cancelada = solicitud.Cancelar(command.Motivo, actual.Id, instante);
        if (cancelada.IsFailure)
        {
            return cancelada;
        }

        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
