using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.Contratos;

internal sealed class ProgramasDeEntidadAcademica(SgplaDbContext contexto) : IProgramasDeEntidadAcademica
{
    public Task<bool> TieneProgramasActivosAsync(int entidadAcademicaId, CancellationToken cancellationToken) =>
        contexto.Set<ProgramaEducativo>().AnyAsync(p => p.EntidadAcademicaId == entidadAcademicaId, cancellationToken);

    public Task<bool> TieneProgramasAsync(int entidadAcademicaId, CancellationToken cancellationToken) =>
        contexto.Set<ProgramaEducativo>()
            .IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
            .AnyAsync(p => p.EntidadAcademicaId == entidadAcademicaId, cancellationToken);
}
