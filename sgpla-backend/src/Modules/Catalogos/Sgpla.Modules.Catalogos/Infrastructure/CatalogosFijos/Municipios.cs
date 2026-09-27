using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.Catalogos.Domain.CatalogosFijos;

namespace Sgpla.Modules.Catalogos.Infrastructure.CatalogosFijos;

internal sealed class Municipios(SgplaDbContext contexto) : IMunicipios
{
    public Task<bool> ExisteAsync(int id, CancellationToken cancellationToken) =>
        contexto.Set<Municipio>().AnyAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyDictionary<int, string>> ObtenerNombresAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        return await contexto.Set<Municipio>()
            .AsNoTracking()
            .Where(m => ids.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Nombre, cancellationToken);
    }
}
