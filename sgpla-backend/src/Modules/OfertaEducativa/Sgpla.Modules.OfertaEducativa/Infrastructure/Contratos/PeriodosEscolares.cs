using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Domain.PeriodosEscolares;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.Contratos;

internal sealed class PeriodosEscolares(SgplaDbContext contexto) : IPeriodosEscolares
{
    public async Task<IReadOnlyDictionary<int, PeriodoEscolarResumen>> ObtenerAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<int, PeriodoEscolarResumen>();
        }

        return await contexto.Set<PeriodoEscolar>()
            .IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new PeriodoEscolarResumen(p.Id, p.Clave, p.FechaInicio, p.FechaFin, p.FechaEliminacion == null))
            .ToDictionaryAsync(p => p.Id, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, PeriodoEscolarResumen>> ObtenerActivosPorClaveAsync(
        IReadOnlyCollection<string> claves,
        CancellationToken cancellationToken)
    {
        if (claves.Count == 0)
        {
            return new Dictionary<string, PeriodoEscolarResumen>(StringComparer.Ordinal);
        }

        return await contexto.Set<PeriodoEscolar>()
            .AsNoTracking()
            .Where(p => claves.Contains(p.Clave))
            .Select(p => new PeriodoEscolarResumen(p.Id, p.Clave, p.FechaInicio, p.FechaFin, true))
            .ToDictionaryAsync(p => p.Clave, StringComparer.Ordinal, cancellationToken);
    }
}
