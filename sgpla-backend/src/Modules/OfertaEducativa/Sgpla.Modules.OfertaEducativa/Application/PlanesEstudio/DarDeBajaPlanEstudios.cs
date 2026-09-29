using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.OfertaEducativa.Application.Ambito;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Application.PlanesEstudio;

internal sealed record DarDeBajaPlanEstudiosCommand(int Id);

/// <summary>
/// Da de baja el plan y todas sus EE activas con el mismo instante (Modulo_OfertaEducativa.md, D10). Se bloquea si alguna
/// EE tiene programaciones activas o referencias de otros módulos (pendientes.md, P7).
/// </summary>
internal sealed class DarDeBajaPlanEstudiosHandler(
    IPlanEstudiosRepository repositorio,
    IAmbitoOfertaEducativa ambito,
    IEnumerable<IReferenciasExperienciaEducativa> referencias,
    IUnitOfWork unidadDeTrabajo,
    TimeProvider reloj) : ICommandHandler<DarDeBajaPlanEstudiosCommand>
{
    public async Task<Result> HandleAsync(DarDeBajaPlanEstudiosCommand command, CancellationToken cancellationToken)
    {
        var plan = await repositorio.ObtenerConExperienciasAsync(command.Id, cancellationToken);
        if (plan is null)
        {
            return PlanEstudiosErrors.NoEncontrado(command.Id);
        }

        var entidadId = await repositorio.ObtenerEntidadDelPlanAsync(plan.Id, cancellationToken);
        if (!await ambito.PuedeEscribirEnEntidadAsync(entidadId, cancellationToken))
        {
            return PlanEstudiosErrors.NoEncontrado(command.Id);
        }

        if (await repositorio.TieneExperienciasConProgramacionesActivasAsync(plan.Id, cancellationToken))
        {
            return PlanEstudiosErrors.ExperienciasConProgramacionesActivas;
        }

        var experienciaIds = plan.ExperienciasEducativas.Select(e => e.Id).ToList();
        foreach (var referencia in referencias)
        {
            if (await referencia.TieneReferenciasAsync(experienciaIds, cancellationToken))
            {
                return ExperienciaEducativaErrors.TieneReferencias;
            }
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var instante = ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond));

        plan.DarDeBaja(instante);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
