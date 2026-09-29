using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.OfertaEducativa.Application.ProgramasEducativos;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;
using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.ProgramasEducativos;

internal sealed class ProgramaEducativoRepository(SgplaDbContext contexto) : IProgramaEducativoRepository
{
    public Task<ProgramaEducativo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        contexto.Set<ProgramaEducativo>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExisteNombreAsync(
        int entidadAcademicaId,
        string nombre,
        int sistemaEducativoId,
        int? excluirId,
        CancellationToken cancellationToken) =>
        contexto.Set<ProgramaEducativo>()
            .IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
            .AnyAsync(
                p => p.EntidadAcademicaId == entidadAcademicaId
                    && p.SistemaEducativoId == sistemaEducativoId
                    && p.Nombre == nombre
                    && (excluirId == null || p.Id != excluirId),
                cancellationToken);

    public Task<bool> TuvoPlanesAsync(int programaEducativoId, CancellationToken cancellationToken) =>
        contexto.Set<PlanEstudios>()
            .IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
            .AnyAsync(p => p.ProgramaEducativoId == programaEducativoId, cancellationToken);

    public Task<bool> TienePlanesActivosAsync(int programaEducativoId, CancellationToken cancellationToken) =>
        contexto.Set<PlanEstudios>().AnyAsync(p => p.ProgramaEducativoId == programaEducativoId, cancellationToken);

    public void Agregar(ProgramaEducativo programaEducativo) => contexto.Set<ProgramaEducativo>().Add(programaEducativo);
}
