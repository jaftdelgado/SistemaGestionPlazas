using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Catalogos.Application.CatalogosFijos;
using Sgpla.Modules.Catalogos.Domain.CatalogosFijos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Infrastructure.CatalogosFijos;

internal sealed class ListarMunicipiosHandler(SgplaDbContext contexto)
    : IQueryHandler<ListarMunicipiosQuery, Pagina<CatalogoFijoResponse>>
{
    public async Task<Result<Pagina<CatalogoFijoResponse>>> HandleAsync(
        ListarMunicipiosQuery query,
        CancellationToken cancellationToken)
    {
        var municipios = contexto.Set<Municipio>().AsNoTracking();
        if (query.Busqueda is not null)
        {
            municipios = municipios.Where(m => m.Nombre.Contains(query.Busqueda));
        }

        return Result.Success(await municipios
            .OrderBy(m => m.Nombre)
            .ThenBy(m => m.Id)
            .Select(m => new CatalogoFijoResponse(m.Id, m.Nombre))
            .PaginarAsync(query.Paginacion, cancellationToken));
    }
}

internal sealed class ObtenerMunicipioHandler(SgplaDbContext contexto)
    : IQueryHandler<ObtenerMunicipioQuery, CatalogoFijoResponse>
{
    public async Task<Result<CatalogoFijoResponse>> HandleAsync(
        ObtenerMunicipioQuery query,
        CancellationToken cancellationToken)
    {
        var municipio = await contexto.Set<Municipio>()
            .AsNoTracking()
            .Where(m => m.Id == query.Id)
            .Select(m => new CatalogoFijoResponse(m.Id, m.Nombre))
            .FirstOrDefaultAsync(cancellationToken);

        return municipio is null ? CatalogoFijoErrors.NoEncontrado<Municipio>(query.Id) : municipio;
    }
}
