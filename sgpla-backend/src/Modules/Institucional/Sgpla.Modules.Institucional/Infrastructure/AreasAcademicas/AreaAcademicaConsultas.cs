using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Institucional.Application.AreasAcademicas;
using Sgpla.Modules.Institucional.Domain.AreasAcademicas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Institucional.Infrastructure.AreasAcademicas;

internal sealed class ListarAreasAcademicasHandler(SgplaDbContext contexto)
    : IQueryHandler<ListarAreasAcademicasQuery, IReadOnlyList<AreaAcademicaResponse>>
{
    public async Task<Result<IReadOnlyList<AreaAcademicaResponse>>> HandleAsync(
        ListarAreasAcademicasQuery query,
        CancellationToken cancellationToken) =>
        Result.Success<IReadOnlyList<AreaAcademicaResponse>>(await contexto.Set<AreaAcademica>()
            .AsNoTracking()
            .OrderBy(a => a.Clave)
            .Select(a => new AreaAcademicaResponse(a.Id, a.Clave, a.Nombre, a.Telefono, a.Extension))
            .ToListAsync(cancellationToken));
}

internal sealed class ObtenerAreaAcademicaHandler(SgplaDbContext contexto)
    : IQueryHandler<ObtenerAreaAcademicaQuery, AreaAcademicaResponse>
{
    public async Task<Result<AreaAcademicaResponse>> HandleAsync(
        ObtenerAreaAcademicaQuery query,
        CancellationToken cancellationToken)
    {
        var area = await contexto.Set<AreaAcademica>()
            .AsNoTracking()
            .Where(a => a.Id == query.Id)
            .Select(a => new AreaAcademicaResponse(a.Id, a.Clave, a.Nombre, a.Telefono, a.Extension))
            .FirstOrDefaultAsync(cancellationToken);

        return area is null ? AreaAcademicaErrors.NoEncontrado(query.Id) : area;
    }
}
