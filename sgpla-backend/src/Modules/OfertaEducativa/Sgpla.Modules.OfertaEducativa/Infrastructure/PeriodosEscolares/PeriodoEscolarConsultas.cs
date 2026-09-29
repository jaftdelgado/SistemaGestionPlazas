using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.OfertaEducativa.Application.PeriodosEscolares;
using Sgpla.Modules.OfertaEducativa.Domain.PeriodosEscolares;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.PeriodosEscolares;

internal sealed class ListarPeriodosEscolaresHandler(SgplaDbContext contexto)
    : IQueryHandler<ListarPeriodosEscolaresQuery, IReadOnlyList<PeriodoEscolarResponse>>
{
    public async Task<Result<IReadOnlyList<PeriodoEscolarResponse>>> HandleAsync(
        ListarPeriodosEscolaresQuery query,
        CancellationToken cancellationToken) =>
        Result.Success<IReadOnlyList<PeriodoEscolarResponse>>(await contexto.Set<PeriodoEscolar>()
            .AsNoTracking()
            .OrderByDescending(p => p.Clave)
            .ThenByDescending(p => p.Id)
            .Select(p => new PeriodoEscolarResponse(p.Id, p.Clave, p.FechaInicio, p.FechaFin))
            .ToListAsync(cancellationToken));
}

internal sealed class ObtenerPeriodoEscolarHandler(SgplaDbContext contexto)
    : IQueryHandler<ObtenerPeriodoEscolarQuery, PeriodoEscolarResponse>
{
    public async Task<Result<PeriodoEscolarResponse>> HandleAsync(
        ObtenerPeriodoEscolarQuery query,
        CancellationToken cancellationToken)
    {
        var periodo = await contexto.Set<PeriodoEscolar>()
            .AsNoTracking()
            .Where(p => p.Id == query.Id)
            .Select(p => new PeriodoEscolarResponse(p.Id, p.Clave, p.FechaInicio, p.FechaFin))
            .FirstOrDefaultAsync(cancellationToken);

        return periodo is null ? PeriodoEscolarErrors.NoEncontrado(query.Id) : periodo;
    }
}
