using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Catalogos.Application.CatalogosFijos;
using Sgpla.Modules.Catalogos.Domain.CatalogosFijos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Infrastructure.CatalogosFijos;

internal sealed class ListarNivelesFormacionHandler(SgplaDbContext contexto)
    : IQueryHandler<ListarNivelesFormacionQuery, IReadOnlyList<ClasificacionConClaveResponse>>
{
    public async Task<Result<IReadOnlyList<ClasificacionConClaveResponse>>> HandleAsync(
        ListarNivelesFormacionQuery query,
        CancellationToken cancellationToken) =>
        Result.Success<IReadOnlyList<ClasificacionConClaveResponse>>(await contexto.Set<NivelFormacion>()
            .AsNoTracking()
            .OrderBy(n => n.Id)
            .Select(n => new ClasificacionConClaveResponse(n.Id, n.Clave, n.Nombre))
            .ToListAsync(cancellationToken));
}

internal sealed class ObtenerNivelFormacionHandler(SgplaDbContext contexto)
    : IQueryHandler<ObtenerNivelFormacionQuery, ClasificacionConClaveResponse>
{
    public async Task<Result<ClasificacionConClaveResponse>> HandleAsync(
        ObtenerNivelFormacionQuery query,
        CancellationToken cancellationToken)
    {
        var encontrado = await contexto.Set<NivelFormacion>()
            .AsNoTracking()
            .Where(n => n.Id == query.Id)
            .Select(n => new ClasificacionConClaveResponse(n.Id, n.Clave, n.Nombre))
            .FirstOrDefaultAsync(cancellationToken);

        return encontrado is null ? CatalogoFijoErrors.NoEncontrado<NivelFormacion>(query.Id) : encontrado;
    }
}
