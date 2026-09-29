using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Domain.PeriodosEscolares;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Application.PeriodosEscolares;

internal sealed record DarDeBajaPeriodoEscolarCommand(int Id);

/// <summary>
/// Sin cascada: la baja se bloquea con cualquier programación del periodo, incluidas las dadas de baja, o con una
/// referencia de otro módulo (Modulo_OfertaEducativa.md, D11; pendientes.md, P8).
/// </summary>
internal sealed class DarDeBajaPeriodoEscolarHandler(
    IPeriodoEscolarRepository repositorio,
    IEnumerable<IReferenciasPeriodoEscolar> referencias,
    IUnitOfWork unidadDeTrabajo,
    TimeProvider reloj) : ICommandHandler<DarDeBajaPeriodoEscolarCommand>
{
    public async Task<Result> HandleAsync(DarDeBajaPeriodoEscolarCommand command, CancellationToken cancellationToken)
    {
        var periodo = await repositorio.ObtenerPorIdAsync(command.Id, cancellationToken);
        if (periodo is null)
        {
            return PeriodoEscolarErrors.NoEncontrado(command.Id);
        }

        if (await repositorio.TieneProgramacionesAsync(periodo.Id, cancellationToken))
        {
            return PeriodoEscolarErrors.TieneReferencias;
        }

        foreach (var referencia in referencias)
        {
            if (await referencia.TieneReferenciasAsync(periodo.Id, cancellationToken))
            {
                return PeriodoEscolarErrors.TieneReferencias;
            }
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var instante = ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond));

        periodo.DarDeBaja(instante);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
