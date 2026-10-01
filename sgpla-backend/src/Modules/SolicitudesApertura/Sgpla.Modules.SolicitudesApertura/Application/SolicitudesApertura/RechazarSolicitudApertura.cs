using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;

internal sealed record RechazarSolicitudAperturaCommand(int Id, string? Comentarios);

internal sealed class RechazarSolicitudAperturaHandler(
    ISolicitudAperturaRepository repositorio,
    IExperienciasEducativas experienciasEducativas,
    ICurrentUser actual,
    IUnitOfWork unidadDeTrabajo,
    TimeProvider reloj) : ICommandHandler<RechazarSolicitudAperturaCommand>
{
    public async Task<Result> HandleAsync(RechazarSolicitudAperturaCommand command, CancellationToken cancellationToken)
    {
        var cargada = await SolicitudAperturaComandos.ObtenerEnAmbitoAsync(
            repositorio,
            experienciasEducativas,
            command.Id,
            e => e.AreaAcademicaId == actual.AreaAcademicaId,
            cancellationToken);
        if (cargada.IsFailure)
        {
            return cargada;
        }

        var rechazada = cargada.Value.Solicitud.Rechazar(
            command.Comentarios, actual.Id, SolicitudAperturaComandos.Ahora(reloj));
        if (rechazada.IsFailure)
        {
            return rechazada;
        }

        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
