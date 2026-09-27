using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Catalogos.Application.Articulos;
using Sgpla.Modules.Catalogos.Domain.Articulos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Infrastructure.Articulos;

internal sealed class ListarArticulosHandler(SgplaDbContext contexto)
    : IQueryHandler<ListarArticulosQuery, IReadOnlyList<ArticuloResponse>>
{
    public async Task<Result<IReadOnlyList<ArticuloResponse>>> HandleAsync(
        ListarArticulosQuery query,
        CancellationToken cancellationToken) =>
        Result.Success<IReadOnlyList<ArticuloResponse>>(await contexto.Set<Articulo>()
            .AsNoTracking()
            .OrderBy(a => a.Numero)
            .ThenBy(a => a.Id)
            .Select(a => new ArticuloResponse(a.Id, a.Numero, a.Descripcion))
            .ToListAsync(cancellationToken));
}

internal sealed class ObtenerArticuloHandler(SgplaDbContext contexto) : IQueryHandler<ObtenerArticuloQuery, ArticuloResponse>
{
    public async Task<Result<ArticuloResponse>> HandleAsync(ObtenerArticuloQuery query, CancellationToken cancellationToken)
    {
        var articulo = await contexto.Set<Articulo>()
            .AsNoTracking()
            .Where(a => a.Id == query.Id)
            .Select(a => new ArticuloResponse(a.Id, a.Numero, a.Descripcion))
            .FirstOrDefaultAsync(cancellationToken);

        return articulo is null ? ArticuloErrors.NoEncontrado(query.Id) : articulo;
    }
}
