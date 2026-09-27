using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Institucional.Application.Ubicaciones;
using Sgpla.Modules.Institucional.Domain.Ubicaciones;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Institucional.Infrastructure.Ubicaciones;

internal sealed class ListarRegionesHandler(SgplaDbContext contexto)
    : IQueryHandler<ListarRegionesQuery, IReadOnlyList<RegionResponse>>
{
    public async Task<Result<IReadOnlyList<RegionResponse>>> HandleAsync(
        ListarRegionesQuery query,
        CancellationToken cancellationToken) =>
        Result.Success<IReadOnlyList<RegionResponse>>(await contexto.Set<Region>()
            .AsNoTracking()
            .OrderBy(r => r.Clave)
            .Select(r => new RegionResponse(r.Id, r.Clave, r.Nombre))
            .ToListAsync(cancellationToken));
}

internal sealed class ObtenerRegionHandler(SgplaDbContext contexto)
    : IQueryHandler<ObtenerRegionQuery, RegionResponse>
{
    public async Task<Result<RegionResponse>> HandleAsync(ObtenerRegionQuery query, CancellationToken cancellationToken)
    {
        var region = await contexto.Set<Region>()
            .AsNoTracking()
            .Where(r => r.Id == query.Id)
            .Select(r => new RegionResponse(r.Id, r.Clave, r.Nombre))
            .FirstOrDefaultAsync(cancellationToken);

        return region is null ? UbicacionErrors.RegionNoEncontrada(query.Id) : region;
    }
}

internal sealed class ListarCampusHandler(SgplaDbContext contexto)
    : IQueryHandler<ListarCampusQuery, IReadOnlyList<CampusResponse>>
{
    public async Task<Result<IReadOnlyList<CampusResponse>>> HandleAsync(
        ListarCampusQuery query,
        CancellationToken cancellationToken)
    {
        var campus = contexto.Set<Campus>().AsQueryable();
        if (query.RegionId is not null)
        {
            campus = campus.Where(c => c.RegionId == query.RegionId);
        }

        return Result.Success<IReadOnlyList<CampusResponse>>(
            await CampusProyeccion.ConRegion(contexto, campus).ToListAsync(cancellationToken));
    }
}

internal sealed class ObtenerCampusHandler(SgplaDbContext contexto)
    : IQueryHandler<ObtenerCampusQuery, CampusResponse>
{
    public async Task<Result<CampusResponse>> HandleAsync(ObtenerCampusQuery query, CancellationToken cancellationToken)
    {
        var encontrado = await CampusProyeccion
            .ConRegion(contexto, contexto.Set<Campus>().Where(c => c.Id == query.Id))
            .FirstOrDefaultAsync(cancellationToken);

        return encontrado is null ? UbicacionErrors.CampusNoEncontrado(query.Id) : encontrado;
    }
}

internal static class CampusProyeccion
{
    /// <summary>Une cada campus con su región, en orden de clave del campus y luego id.</summary>
    public static IQueryable<CampusResponse> ConRegion(SgplaDbContext contexto, IQueryable<Campus> campus) =>
        from c in campus.AsNoTracking()
        join r in contexto.Set<Region>().AsNoTracking() on c.RegionId equals r.Id
        orderby c.Clave, c.Id
        select new CampusResponse(c.Id, c.Clave, c.Nombre, new RegionResponse(r.Id, r.Clave, r.Nombre));
}
