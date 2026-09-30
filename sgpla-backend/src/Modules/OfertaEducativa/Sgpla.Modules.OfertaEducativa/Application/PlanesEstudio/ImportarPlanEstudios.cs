using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Application.Ambito;
using Sgpla.Modules.OfertaEducativa.Application.ExperienciasEducativas;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Application.PlanesEstudio;

internal sealed record ImportarPlanEstudiosCommand(
    int ProgramaEducativoId,
    string Codigo,
    IReadOnlyList<ExperienciaEducativaEntrada?> ExperienciasEducativas);

/// <summary>
/// Crea el plan con todas sus EE o no crea nada (Modulo_OfertaEducativa.md, D6). Primero valida la forma de todo el plan
/// (dominio) y después las referencias; un programa fuera del ámbito del DGAA se trata como inexistente (D9 y D15).
/// </summary>
internal sealed class ImportarPlanEstudiosHandler(
    IPlanEstudiosRepository repositorio,
    IAmbitoOfertaEducativa ambito,
    IClasificacionesAcademicas clasificaciones,
    IUnitOfWork unidadDeTrabajo) : ICommandHandler<ImportarPlanEstudiosCommand, int>
{
    public async Task<Result<int>> HandleAsync(ImportarPlanEstudiosCommand command, CancellationToken cancellationToken)
    {
        var creado = PlanEstudios.Crear(
            command.Codigo, command.ProgramaEducativoId, command.ExperienciasEducativas.Select(e => e?.ADatos()).ToList());
        if (creado.IsFailure)
        {
            return creado.Error;
        }

        var plan = creado.Value;

        var entidadId = await repositorio.ObtenerEntidadDeProgramaActivoAsync(plan.ProgramaEducativoId, cancellationToken);
        if (entidadId is null || !await ambito.PuedeEscribirEnEntidadAsync(entidadId.Value, cancellationToken))
        {
            return PlanEstudiosErrors.ProgramaEducativoInexistente;
        }

        var experiencias = plan.ExperienciasEducativas.ToList();
        var areas = await clasificaciones.ObtenerAreasFormacionAsync(
            experiencias.Select(e => e.AreaFormacionId).Distinct().ToList(), cancellationToken);
        var indiceSinArea = experiencias.FindIndex(e => !areas.ContainsKey(e.AreaFormacionId));
        if (indiceSinArea >= 0)
        {
            return PlanEstudiosErrors.AreaFormacionInexistente(indiceSinArea);
        }

        if (await repositorio.ExisteCodigoAsync(plan.ProgramaEducativoId, plan.Codigo, cancellationToken))
        {
            return PlanEstudiosErrors.CodigoDuplicado;
        }

        repositorio.Agregar(plan);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);

        return plan.Id;
    }
}
