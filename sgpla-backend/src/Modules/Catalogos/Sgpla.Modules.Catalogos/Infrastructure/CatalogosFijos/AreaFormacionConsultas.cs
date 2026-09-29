using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Catalogos.Application.CatalogosFijos;
using Sgpla.Modules.Catalogos.Domain.CatalogosFijos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Infrastructure.CatalogosFijos;

internal sealed class ListarAreasFormacionHandler(SgplaDbContext contexto)
    : IQueryHandler<ListarAreasFormacionQuery, IReadOnlyList<ClasificacionConClaveResponse>>
{
    public async Task<Result<IReadOnlyList<ClasificacionConClaveResponse>>> HandleAsync(
        ListarAreasFormacionQuery query,
        CancellationToken cancellationToken) =>
        Result.Success<IReadOnlyList<ClasificacionConClaveResponse>>(await contexto.Set<AreaFormacion>()
            .AsNoTracking()
            .OrderBy(a => a.Id)
            .Select(a => new ClasificacionConClaveResponse(a.Id, a.Clave, a.Nombre))
            .ToListAsync(cancellationToken));
}

internal sealed class ObtenerAreaFormacionHandler(SgplaDbContext contexto)
    : IQueryHandler<ObtenerAreaFormacionQuery, ClasificacionConClaveResponse>
{
    public async Task<Result<ClasificacionConClaveResponse>> HandleAsync(
        ObtenerAreaFormacionQuery query,
        CancellationToken cancellationToken)
    {
        var encontrada = await contexto.Set<AreaFormacion>()
            .AsNoTracking()
            .Where(a => a.Id == query.Id)
            .Select(a => new ClasificacionConClaveResponse(a.Id, a.Clave, a.Nombre))
            .FirstOrDefaultAsync(cancellationToken);

        return encontrada is null ? CatalogoFijoErrors.NoEncontrado<AreaFormacion>(query.Id) : encontrada;
    }
}
