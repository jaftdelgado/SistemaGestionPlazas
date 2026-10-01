using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;

internal sealed record AceptarSolicitudAperturaCommand(int Id, string? Comentarios);

internal sealed class AceptarSolicitudAperturaHandler(
    ISolicitudAperturaRepository repositorio,
    IExperienciasEducativas experienciasEducativas,
    ICurrentUser actual,
    IUnitOfWork unidadDeTrabajo,
    TimeProvider reloj) : ICommandHandler<AceptarSolicitudAperturaCommand>
{
    public async Task<Result> HandleAsync(AceptarSolicitudAperturaCommand command, CancellationToken cancellationToken)
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

        var (solicitud, experiencia) = cargada.Value;
        var aceptada = solicitud.Aceptar(
            command.Comentarios,
            experiencia.CupoMinimo,
            experiencia.CupoMaximo,
            actual.Id,
            SolicitudAperturaComandos.Ahora(reloj));
        if (aceptada.IsFailure)
        {
            return aceptada;
        }

        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
