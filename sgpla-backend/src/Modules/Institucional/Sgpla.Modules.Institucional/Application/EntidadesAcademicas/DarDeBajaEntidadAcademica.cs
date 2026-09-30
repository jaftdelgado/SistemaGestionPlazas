using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.Institucional.Domain.EntidadesAcademicas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Institucional.Application.EntidadesAcademicas;

internal sealed record DarDeBajaEntidadAcademicaCommand(int Id);

/// <summary>
/// Sin cascada: la baja se bloquea con usuarios activos o con programas educativos activos.
/// </summary>
internal sealed class DarDeBajaEntidadAcademicaHandler(
    IEntidadAcademicaRepository repositorio,
    IEnumerable<IUsuariosDeEntidadAcademica> usuarios,
    IEnumerable<IProgramasDeEntidadAcademica> programas,
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

        foreach (var contrato in usuarios)
        {
            if (await contrato.TieneUsuariosActivosAsync(entidad.Id, cancellationToken))
            {
                return EntidadAcademicaErrors.TieneUsuariosActivos;
            }
        }

        foreach (var contrato in programas)
        {
            if (await contrato.TieneProgramasActivosAsync(entidad.Id, cancellationToken))
            {
                return EntidadAcademicaErrors.TieneProgramasActivos;
            }
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var instante = ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond));

        entidad.DarDeBaja(instante);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
