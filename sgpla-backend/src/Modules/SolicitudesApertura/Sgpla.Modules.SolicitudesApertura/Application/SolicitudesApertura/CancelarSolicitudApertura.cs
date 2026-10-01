using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;

internal sealed record CancelarSolicitudAperturaCommand(int Id, string? Motivo);

internal sealed class CancelarSolicitudAperturaHandler(
    ISolicitudAperturaRepository repositorio,
    IExperienciasEducativas experienciasEducativas,
    ICurrentUser actual,
    IUnitOfWork unidadDeTrabajo,
    TimeProvider reloj) : ICommandHandler<CancelarSolicitudAperturaCommand>
{
    public async Task<Result> HandleAsync(CancelarSolicitudAperturaCommand command, CancellationToken cancellationToken)
    {
        var cargada = await SolicitudAperturaComandos.ObtenerEnAmbitoAsync(
            repositorio,
            experienciasEducativas,
            command.Id,
            e => e.EntidadAcademicaId == actual.EntidadAcademicaId,
            cancellationToken);
        if (cargada.IsFailure)
        {
            return cargada;
        }

        var cancelada = cargada.Value.Solicitud.Cancelar(
            command.Motivo, actual.Id, SolicitudAperturaComandos.Ahora(reloj));
        if (cancelada.IsFailure)
        {
            return cancelada;
        }

        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
