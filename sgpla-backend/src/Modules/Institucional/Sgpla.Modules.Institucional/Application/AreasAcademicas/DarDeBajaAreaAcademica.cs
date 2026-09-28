using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.Institucional.Domain.AreasAcademicas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Institucional.Application.AreasAcademicas;

internal sealed record DarDeBajaAreaAcademicaCommand(int Id);

internal sealed class DarDeBajaAreaAcademicaHandler(
    IAreaAcademicaRepository repositorio,
    IEnumerable<IUsuariosDeAreaAcademica> usuarios,
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

        foreach (var contrato in usuarios)
        {
            if (await contrato.TieneUsuariosActivosAsync(area.Id, cancellationToken))
            {
                return AreaAcademicaErrors.TieneUsuariosActivos;
            }
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var instante = ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond));

        area.DarDeBaja(instante);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
