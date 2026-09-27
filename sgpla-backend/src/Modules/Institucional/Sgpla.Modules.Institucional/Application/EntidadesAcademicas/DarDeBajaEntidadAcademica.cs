using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Institucional.Domain.EntidadesAcademicas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Institucional.Application.EntidadesAcademicas;

internal sealed record DarDeBajaEntidadAcademicaCommand(int Id);

/// <summary>Sin cascada ni bloqueos todavía (Modulo_Institucional.md, decisión D6).</summary>
internal sealed class DarDeBajaEntidadAcademicaHandler(
    IEntidadAcademicaRepository repositorio,
    IUnitOfWork unidadDeTrabajo,
    TimeProvider reloj) : ICommandHandler<DarDeBajaEntidadAcademicaCommand>
{
    public async Task<Result> HandleAsync(DarDeBajaEntidadAcademicaCommand command, CancellationToken cancellationToken)
    {
        var entidad = await repositorio.ObtenerPorIdAsync(command.Id, cancellationToken);
        if (entidad is null)
        {
            return EntidadAcademicaErrors.NoEncontrado(command.Id);
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var instante = ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond));

        entidad.DarDeBaja(instante);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
