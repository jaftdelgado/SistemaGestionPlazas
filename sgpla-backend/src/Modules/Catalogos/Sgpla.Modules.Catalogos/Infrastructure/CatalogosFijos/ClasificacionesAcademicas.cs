using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.Catalogos.Domain.CatalogosFijos;

namespace Sgpla.Modules.Catalogos.Infrastructure.CatalogosFijos;

internal sealed class ClasificacionesAcademicas(SgplaDbContext contexto) : IClasificacionesAcademicas
{
    public async Task<IReadOnlyDictionary<int, SistemaEducativoResumen>> ObtenerSistemasEducativosAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<int, SistemaEducativoResumen>();
        }

        return await contexto.Set<SistemaEducativo>()
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .Select(s => new SistemaEducativoResumen(s.Id, s.Nombre))
            .ToDictionaryAsync(resumen => resumen.Id, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, NivelFormacionResumen>> ObtenerNivelesFormacionAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<int, NivelFormacionResumen>();
        }

        return await contexto.Set<NivelFormacion>()
            .AsNoTracking()
            .Where(n => ids.Contains(n.Id))
            .Select(n => new NivelFormacionResumen(n.Id, n.Clave, n.Nombre))
            .ToDictionaryAsync(resumen => resumen.Id, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, AreaFormacionResumen>> ObtenerAreasFormacionAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<int, AreaFormacionResumen>();
        }

        return await contexto.Set<AreaFormacion>()
            .AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .Select(a => new AreaFormacionResumen(a.Id, a.Clave, a.Nombre))
            .ToDictionaryAsync(resumen => resumen.Id, cancellationToken);
    }
}
