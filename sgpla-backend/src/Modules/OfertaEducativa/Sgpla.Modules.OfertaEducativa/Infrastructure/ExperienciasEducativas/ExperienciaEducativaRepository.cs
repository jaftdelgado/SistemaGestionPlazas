using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.OfertaEducativa.Application.ExperienciasEducativas;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;
using Sgpla.Modules.OfertaEducativa.Domain.Programaciones;
using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.ExperienciasEducativas;

internal sealed class ExperienciaEducativaRepository(SgplaDbContext contexto) : IExperienciaEducativaRepository
{
    public Task<ExperienciaEducativa?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        contexto.Set<ExperienciaEducativa>().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<bool> ExisteMateriaCursoAsync(
        int planEstudiosId,
        string materia,
        string curso,
        CancellationToken cancellationToken) =>
        contexto.Set<ExperienciaEducativa>()
            .IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
            .AnyAsync(
                e => e.PlanEstudiosId == planEstudiosId && e.Materia == materia && e.Curso == curso,
                cancellationToken);

    public Task<bool> TuvoProgramacionesAsync(int experienciaEducativaId, CancellationToken cancellationToken) =>
        contexto.Set<ProgramacionAcademica>()
            .IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
            .AnyAsync(p => p.ExperienciaEducativaId == experienciaEducativaId, cancellationToken);

    public Task<bool> TieneProgramacionesActivasAsync(int experienciaEducativaId, CancellationToken cancellationToken) =>
        contexto.Set<ProgramacionAcademica>()
            .AnyAsync(p => p.ExperienciaEducativaId == experienciaEducativaId, cancellationToken);

    public Task<int> ObtenerEntidadAsync(int experienciaEducativaId, CancellationToken cancellationToken) =>
        (from experiencia in contexto.Set<ExperienciaEducativa>().IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
         join plan in contexto.Set<PlanEstudios>().IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
             on experiencia.PlanEstudiosId equals plan.Id
         join programa in contexto.Set<ProgramaEducativo>().IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
             on plan.ProgramaEducativoId equals programa.Id
         where experiencia.Id == experienciaEducativaId
         select programa.EntidadAcademicaId)
        .FirstAsync(cancellationToken);
}
