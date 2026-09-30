using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.OfertaEducativa.Application.Ambito;
using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Application.ProgramasEducativos;

internal sealed record DarDeBajaProgramaEducativoCommand(int Id);

/// <summary>Sin cascada: la baja se bloquea con planes de estudio activos.</summary>
internal sealed class DarDeBajaProgramaEducativoHandler(
    IProgramaEducativoRepository repositorio,
    IAmbitoOfertaEducativa ambito,
    IUnitOfWork unidadDeTrabajo,
    TimeProvider reloj) : ICommandHandler<DarDeBajaProgramaEducativoCommand>
{
    public async Task<Result> HandleAsync(DarDeBajaProgramaEducativoCommand command, CancellationToken cancellationToken)
    {
        var programa = await repositorio.ObtenerPorIdAsync(command.Id, cancellationToken);
        if (programa is null || !await ambito.PuedeEscribirEnEntidadAsync(programa.EntidadAcademicaId, cancellationToken))
        {
            return ProgramaEducativoErrors.NoEncontrado(command.Id);
        }

        if (await repositorio.TienePlanesActivosAsync(programa.Id, cancellationToken))
        {
            return ProgramaEducativoErrors.TienePlanesActivos;
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var instante = ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond));

        programa.DarDeBaja(instante);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
