using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.OfertaEducativa.Domain.PeriodosEscolares;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Application.PeriodosEscolares;

internal sealed record ModificarPeriodoEscolarCommand(int Id, DateOnly FechaInicio, DateOnly FechaFin);

/// <summary>Las fechas siempre pueden cambiar, aunque el periodo tenga programaciones (Modulo_OfertaEducativa.md, D11).</summary>
internal sealed class ModificarPeriodoEscolarHandler(
    IPeriodoEscolarRepository repositorio,
    IUnitOfWork unidadDeTrabajo) : ICommandHandler<ModificarPeriodoEscolarCommand>
{
    public async Task<Result> HandleAsync(ModificarPeriodoEscolarCommand command, CancellationToken cancellationToken)
    {
        var periodo = await repositorio.ObtenerPorIdAsync(command.Id, cancellationToken);
        if (periodo is null)
        {
            return PeriodoEscolarErrors.NoEncontrado(command.Id);
        }

        var modificado = periodo.Modificar(command.FechaInicio, command.FechaFin);
        if (modificado.IsFailure)
        {
            return modificado;
        }

        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
