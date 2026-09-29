using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.OfertaEducativa.Domain.PeriodosEscolares;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Application.PeriodosEscolares;

internal sealed record CrearPeriodoEscolarCommand(string Clave, DateOnly FechaInicio, DateOnly FechaFin);

/// <summary>La forma de la entrada la valida <see cref="PeriodoEscolar.Crear"/>; aquí solo queda la unicidad de la clave.</summary>
internal sealed class CrearPeriodoEscolarHandler(
    IPeriodoEscolarRepository repositorio,
    IUnitOfWork unidadDeTrabajo) : ICommandHandler<CrearPeriodoEscolarCommand, PeriodoEscolarResponse>
{
    public async Task<Result<PeriodoEscolarResponse>> HandleAsync(
        CrearPeriodoEscolarCommand command,
        CancellationToken cancellationToken)
    {
        var creado = PeriodoEscolar.Crear(command.Clave, command.FechaInicio, command.FechaFin);
        if (creado.IsFailure)
        {
            return creado.Error;
        }

        var periodo = creado.Value;

        if (await repositorio.ExisteClaveAsync(periodo.Clave, cancellationToken))
        {
            return PeriodoEscolarErrors.ClaveDuplicada;
        }

        repositorio.Agregar(periodo);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);

        return PeriodoEscolarResponse.Desde(periodo);
    }
}
