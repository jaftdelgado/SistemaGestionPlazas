using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.OfertaEducativa.Application.PlanesEstudio;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;
using Sgpla.Modules.OfertaEducativa.Domain.Programaciones;
using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.PlanesEstudio;

internal sealed class PlanEstudiosRepository(SgplaDbContext contexto) : IPlanEstudiosRepository
{
    public Task<PlanEstudios?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        contexto.Set<PlanEstudios>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <remarks>El filtro global de <see cref="ExperienciaEducativa"/> deja fuera las EE dadas de baja.</remarks>
    public Task<PlanEstudios?> ObtenerConExperienciasAsync(int id, CancellationToken cancellationToken) =>
        contexto.Set<PlanEstudios>()
            .Include(p => p.ExperienciasEducativas)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExisteCodigoAsync(int programaEducativoId, string codigo, CancellationToken cancellationToken) =>
        contexto.Set<PlanEstudios>()
            .IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
            .AnyAsync(p => p.ProgramaEducativoId == programaEducativoId && p.Codigo == codigo, cancellationToken);

    public Task<int?> ObtenerEntidadDeProgramaActivoAsync(int programaEducativoId, CancellationToken cancellationToken) =>
        contexto.Set<ProgramaEducativo>()
            .Where(p => p.Id == programaEducativoId)
            .Select(p => (int?)p.EntidadAcademicaId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<int> ObtenerEntidadDelPlanAsync(int planEstudiosId, CancellationToken cancellationToken) =>
        (from plan in contexto.Set<PlanEstudios>().IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
         join programa in contexto.Set<ProgramaEducativo>().IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
             on plan.ProgramaEducativoId equals programa.Id
         where plan.Id == planEstudiosId
         select programa.EntidadAcademicaId)
        .FirstAsync(cancellationToken);

    public Task<bool> TieneExperienciasConProgramacionesActivasAsync(int planEstudiosId, CancellationToken cancellationToken) =>
        (from programacion in contexto.Set<ProgramacionAcademica>()
         join experiencia in contexto.Set<ExperienciaEducativa>()
             on programacion.ExperienciaEducativaId equals experiencia.Id
         where experiencia.PlanEstudiosId == planEstudiosId
         select programacion.Id)
        .AnyAsync(cancellationToken);

    public void Agregar(PlanEstudios planEstudios) => contexto.Set<PlanEstudios>().Add(planEstudios);
}
