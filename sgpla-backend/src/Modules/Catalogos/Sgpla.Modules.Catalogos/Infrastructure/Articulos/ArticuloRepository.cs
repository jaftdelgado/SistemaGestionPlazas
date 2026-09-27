using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Catalogos.Application.Articulos;
using Sgpla.Modules.Catalogos.Domain.Articulos;

namespace Sgpla.Modules.Catalogos.Infrastructure.Articulos;

internal sealed class ArticuloRepository(SgplaDbContext contexto) : IArticuloRepository
{
    public Task<Articulo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        contexto.Set<Articulo>().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> ExisteNumeroAsync(string numero, int? excluirId, CancellationToken cancellationToken) =>
        contexto.Set<Articulo>()
            .AnyAsync(a => a.Numero == numero && (excluirId == null || a.Id != excluirId), cancellationToken);

    public void Agregar(Articulo articulo) => contexto.Set<Articulo>().Add(articulo);
}
