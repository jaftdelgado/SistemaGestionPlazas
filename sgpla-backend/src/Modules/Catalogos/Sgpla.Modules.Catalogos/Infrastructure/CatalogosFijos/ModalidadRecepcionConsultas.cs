using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Catalogos.Application.CatalogosFijos;
using Sgpla.Modules.Catalogos.Domain.CatalogosFijos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Infrastructure.CatalogosFijos;

internal sealed class ListarModalidadesRecepcionHandler(SgplaDbContext contexto)
    : IQueryHandler<ListarModalidadesRecepcionQuery, IReadOnlyList<ModalidadRecepcionResponse>>
{
    public async Task<Result<IReadOnlyList<ModalidadRecepcionResponse>>> HandleAsync(
        ListarModalidadesRecepcionQuery query,
        CancellationToken cancellationToken) =>
        Result.Success<IReadOnlyList<ModalidadRecepcionResponse>>(await contexto.Set<ModalidadRecepcion>()
            .AsNoTracking()
            .OrderBy(m => m.Id)
            .Select(m => new ModalidadRecepcionResponse(m.Id, m.Nombre, m.RequiereLugar))
            .ToListAsync(cancellationToken));
}

internal sealed class ObtenerModalidadRecepcionHandler(SgplaDbContext contexto)
    : IQueryHandler<ObtenerModalidadRecepcionQuery, ModalidadRecepcionResponse>
{
    public async Task<Result<ModalidadRecepcionResponse>> HandleAsync(
        ObtenerModalidadRecepcionQuery query,
        CancellationToken cancellationToken)
    {
        var encontrada = await contexto.Set<ModalidadRecepcion>()
            .AsNoTracking()
            .Where(m => m.Id == query.Id)
            .Select(m => new ModalidadRecepcionResponse(m.Id, m.Nombre, m.RequiereLugar))
            .FirstOrDefaultAsync(cancellationToken);

        return encontrada is null ? CatalogoFijoErrors.NoEncontrado<ModalidadRecepcion>(query.Id) : encontrada;
    }
}
