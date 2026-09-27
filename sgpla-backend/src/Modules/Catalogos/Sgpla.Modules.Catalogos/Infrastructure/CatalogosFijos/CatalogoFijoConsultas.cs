using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Catalogos.Application.CatalogosFijos;
using Sgpla.Modules.Catalogos.Domain.CatalogosFijos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Infrastructure.CatalogosFijos;

// Genéricos: el escaneo de AddHandlersModulo no los registra; CatalogosModule lo hace por cada catálogo.

internal sealed class ListarCatalogoFijoHandler<TCatalogo>(SgplaDbContext contexto)
    : IQueryHandler<ListarCatalogoFijoQuery<TCatalogo>, IReadOnlyList<CatalogoFijoResponse>>
    where TCatalogo : CatalogoFijo
{
    public async Task<Result<IReadOnlyList<CatalogoFijoResponse>>> HandleAsync(
        ListarCatalogoFijoQuery<TCatalogo> query,
        CancellationToken cancellationToken) =>
        Result.Success<IReadOnlyList<CatalogoFijoResponse>>(await contexto.Set<TCatalogo>()
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new CatalogoFijoResponse(c.Id, c.Nombre))
            .ToListAsync(cancellationToken));
}

internal sealed class ObtenerCatalogoFijoHandler<TCatalogo>(SgplaDbContext contexto)
    : IQueryHandler<ObtenerCatalogoFijoQuery<TCatalogo>, CatalogoFijoResponse>
    where TCatalogo : CatalogoFijo
{
    public async Task<Result<CatalogoFijoResponse>> HandleAsync(
        ObtenerCatalogoFijoQuery<TCatalogo> query,
        CancellationToken cancellationToken)
    {
        var encontrado = await contexto.Set<TCatalogo>()
            .AsNoTracking()
            .Where(c => c.Id == query.Id)
            .Select(c => new CatalogoFijoResponse(c.Id, c.Nombre))
            .FirstOrDefaultAsync(cancellationToken);

        return encontrado is null ? CatalogoFijoErrors.NoEncontrado<TCatalogo>(query.Id) : encontrado;
    }
}
