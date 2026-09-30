using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Application.Ambito;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Application.ExperienciasEducativas;

/// <summary>Reemplazo completo de lo editable: un cupo o un perfil en <c>null</c> los quita. La materia, el curso y el plan no cambian.</summary>
internal sealed record ModificarExperienciaEducativaCommand(
    int Id,
    string Nombre,
    int HorasTeoricas,
    int HorasPracticas,
    int Creditos,
    int? CupoMinimo,
    int? CupoMaximo,
    string? PerfilDocente,
    int AreaFormacionId);

/// <summary>
/// Nombre, perfil y cupos cambian siempre; las horas, los créditos y el área, solo si la EE nunca tuvo una programación,
/// incluidas las dadas de baja. Una EE fuera del ámbito del DGAA responde
/// <c>NoEncontrado</c> (D15). Si algo falla después de <see cref="ExperienciaEducativa.Modificar"/>, no se guarda.
/// </summary>
internal sealed class ModificarExperienciaEducativaHandler(
    IExperienciaEducativaRepository repositorio,
    IAmbitoOfertaEducativa ambito,
    IClasificacionesAcademicas clasificaciones,
    IUnitOfWork unidadDeTrabajo) : ICommandHandler<ModificarExperienciaEducativaCommand>
{
    public async Task<Result> HandleAsync(ModificarExperienciaEducativaCommand command, CancellationToken cancellationToken)
    {
        var experiencia = await repositorio.ObtenerPorIdAsync(command.Id, cancellationToken);
        if (experiencia is null
            || !await ambito.PuedeEscribirEnEntidadAsync(
                await repositorio.ObtenerEntidadAsync(experiencia.Id, cancellationToken), cancellationToken))
        {
            return ExperienciaEducativaErrors.NoEncontrado(command.Id);
        }

        var tuvoProgramaciones = await repositorio.TuvoProgramacionesAsync(experiencia.Id, cancellationToken);

        var modificada = experiencia.Modificar(
            command.Nombre,
            command.HorasTeoricas,
            command.HorasPracticas,
            command.Creditos,
            command.CupoMinimo,
            command.CupoMaximo,
            command.PerfilDocente,
            command.AreaFormacionId,
            tuvoProgramaciones);
        if (modificada.IsFailure)
        {
            return modificada;
        }

        var areas = await clasificaciones.ObtenerAreasFormacionAsync([experiencia.AreaFormacionId], cancellationToken);
        if (!areas.ContainsKey(experiencia.AreaFormacionId))
        {
            return ExperienciaEducativaErrors.AreaFormacionInexistente;
        }

        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
