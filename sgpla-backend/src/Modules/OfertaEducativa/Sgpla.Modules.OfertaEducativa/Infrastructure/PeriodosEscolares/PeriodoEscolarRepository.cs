using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.OfertaEducativa.Application.PeriodosEscolares;
using Sgpla.Modules.OfertaEducativa.Domain.PeriodosEscolares;
using Sgpla.Modules.OfertaEducativa.Domain.Programaciones;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.PeriodosEscolares;

internal sealed class PeriodoEscolarRepository(SgplaDbContext contexto) : IPeriodoEscolarRepository
{
    public Task<PeriodoEscolar?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        contexto.Set<PeriodoEscolar>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExisteClaveAsync(string clave, CancellationToken cancellationToken) =>
        contexto.Set<PeriodoEscolar>()
            .IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
            .AnyAsync(p => p.Clave == clave, cancellationToken);

    public Task<bool> TieneProgramacionesAsync(int periodoEscolarId, CancellationToken cancellationToken) =>
        contexto.Set<ProgramacionAcademica>()
            .IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
            .AnyAsync(p => p.PeriodoEscolarId == periodoEscolarId, cancellationToken);

    public void Agregar(PeriodoEscolar periodoEscolar) => contexto.Set<PeriodoEscolar>().Add(periodoEscolar);
}
