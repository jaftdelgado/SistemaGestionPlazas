using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.OfertaEducativa.Application.Ambito;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Application.ExperienciasEducativas;

internal sealed record DarDeBajaExperienciaEducativaCommand(int Id);

/// <summary>
/// Sin cascada: la baja se bloquea con programaciones activas o con referencias de otros
/// módulos. Dar de baja la última EE activa no da de baja el plan.
/// </summary>
internal sealed class DarDeBajaExperienciaEducativaHandler(
    IExperienciaEducativaRepository repositorio,
    IAmbitoOfertaEducativa ambito,
    IEnumerable<IReferenciasExperienciaEducativa> referencias,
    IUnitOfWork unidadDeTrabajo,
    TimeProvider reloj) : ICommandHandler<DarDeBajaExperienciaEducativaCommand>
{
    public async Task<Result> HandleAsync(DarDeBajaExperienciaEducativaCommand command, CancellationToken cancellationToken)
    {
        var experiencia = await repositorio.ObtenerPorIdAsync(command.Id, cancellationToken);
        if (experiencia is null
            || !await ambito.PuedeEscribirEnEntidadAsync(
                await repositorio.ObtenerEntidadAsync(experiencia.Id, cancellationToken), cancellationToken))
        {
            return ExperienciaEducativaErrors.NoEncontrado(command.Id);
        }

        if (await repositorio.TieneProgramacionesActivasAsync(experiencia.Id, cancellationToken))
        {
            return ExperienciaEducativaErrors.TieneProgramacionesActivas;
        }

        foreach (var referencia in referencias)
        {
            if (await referencia.TieneReferenciasAsync([experiencia.Id], cancellationToken))
            {
                return ExperienciaEducativaErrors.TieneReferencias;
            }
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var instante = ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond));

        experiencia.DarDeBaja(instante);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
