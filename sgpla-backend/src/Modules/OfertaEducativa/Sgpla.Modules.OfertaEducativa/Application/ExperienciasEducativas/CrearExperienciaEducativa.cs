using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Application.Ambito;
using Sgpla.Modules.OfertaEducativa.Application.PlanesEstudio;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Application.ExperienciasEducativas;

internal sealed record CrearExperienciaEducativaCommand(int PlanEstudiosId, ExperienciaEducativaEntrada Datos);

/// <summary>
/// Agrega una EE a un plan activo del área del DGAA; un plan fuera de su ámbito se trata como inexistente (D9 y D15).
/// La combinación de materia y curso no se reutiliza, ni siquiera con una EE dada de baja.
/// </summary>
internal sealed class CrearExperienciaEducativaHandler(
    IPlanEstudiosRepository planes,
    IExperienciaEducativaRepository repositorio,
    IAmbitoOfertaEducativa ambito,
    IClasificacionesAcademicas clasificaciones,
    IUnitOfWork unidadDeTrabajo) : ICommandHandler<CrearExperienciaEducativaCommand, int>
{
    public async Task<Result<int>> HandleAsync(CrearExperienciaEducativaCommand command, CancellationToken cancellationToken)
    {
        var plan = await planes.ObtenerPorIdAsync(command.PlanEstudiosId, cancellationToken);
        if (plan is null
            || !await ambito.PuedeEscribirEnEntidadAsync(
                await planes.ObtenerEntidadDelPlanAsync(plan.Id, cancellationToken), cancellationToken))
        {
            return ExperienciaEducativaErrors.PlanEstudiosInexistente;
        }

        var agregada = plan.AgregarExperiencia(command.Datos.ADatos());
        if (agregada.IsFailure)
        {
            return agregada.Error;
        }

        var experiencia = agregada.Value;

        var areas = await clasificaciones.ObtenerAreasFormacionAsync([experiencia.AreaFormacionId], cancellationToken);
        if (!areas.ContainsKey(experiencia.AreaFormacionId))
        {
            return ExperienciaEducativaErrors.AreaFormacionInexistente;
        }

        if (await repositorio.ExisteMateriaCursoAsync(plan.Id, experiencia.Materia, experiencia.Curso, cancellationToken))
        {
            return ExperienciaEducativaErrors.MateriaCursoDuplicado;
        }

        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);

        return experiencia.Id;
    }
}
