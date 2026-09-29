using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.OfertaEducativa.Application.ProgramasEducativos;
using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.ProgramasEducativos;

/// <remarks>
/// Los planes de estudio se consultan con SQL de solo lectura porque su entidad llega en el PR 3 del módulo
/// (Modulo_OfertaEducativa.md, sección 15); entonces estas dos consultas pasan a usarla.
/// </remarks>
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
        contexto.Database
            .SqlQuery<int>($"SELECT id AS [Value] FROM academico.plan_estudios WHERE programa_educativo_id = {programaEducativoId}")
            .AnyAsync(cancellationToken);

    public Task<bool> TienePlanesActivosAsync(int programaEducativoId, CancellationToken cancellationToken) =>
        contexto.Database
            .SqlQuery<int>(
                $"SELECT id AS [Value] FROM academico.plan_estudios WHERE programa_educativo_id = {programaEducativoId} AND fecha_eliminacion IS NULL")
            .AnyAsync(cancellationToken);

    public void Agregar(ProgramaEducativo programaEducativo) => contexto.Set<ProgramaEducativo>().Add(programaEducativo);
}
