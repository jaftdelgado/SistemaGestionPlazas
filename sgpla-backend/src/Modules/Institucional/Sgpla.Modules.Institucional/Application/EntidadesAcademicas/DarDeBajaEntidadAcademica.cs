using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.Institucional.Domain.EntidadesAcademicas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Institucional.Application.EntidadesAcademicas;

internal sealed record DarDeBajaEntidadAcademicaCommand(int Id);

/// <summary>Sin cascada todavía (Modulo_Institucional.md, decisión D6; pendientes.md, P1).</summary>
internal sealed class DarDeBajaEntidadAcademicaHandler(
    IEntidadAcademicaRepository repositorio,
    IEnumerable<IUsuariosDeEntidadAcademica> usuarios,
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

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var instante = ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond));

        entidad.DarDeBaja(instante);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
