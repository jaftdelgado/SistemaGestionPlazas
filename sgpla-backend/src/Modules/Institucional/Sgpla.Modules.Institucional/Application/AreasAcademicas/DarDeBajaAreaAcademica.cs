using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Institucional.Domain.AreasAcademicas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Institucional.Application.AreasAcademicas;

internal sealed record DarDeBajaAreaAcademicaCommand(int Id);

internal sealed class DarDeBajaAreaAcademicaHandler(
    IAreaAcademicaRepository repositorio,
    IUnitOfWork unidadDeTrabajo,
    TimeProvider reloj) : ICommandHandler<DarDeBajaAreaAcademicaCommand>
{
    public async Task<Result> HandleAsync(DarDeBajaAreaAcademicaCommand command, CancellationToken cancellationToken)
    {
        var area = await repositorio.ObtenerPorIdAsync(command.Id, cancellationToken);
        if (area is null)
        {
            return AreaAcademicaErrors.NoEncontrado(command.Id);
        }

        if (await repositorio.TieneEntidadesActivasAsync(area.Id, cancellationToken))
        {
            return AreaAcademicaErrors.TieneEntidadesActivas;
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var instante = ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond));

        area.DarDeBaja(instante);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
