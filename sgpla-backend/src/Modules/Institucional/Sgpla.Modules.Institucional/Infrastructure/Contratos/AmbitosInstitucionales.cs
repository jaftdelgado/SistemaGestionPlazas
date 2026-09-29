using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.Institucional.Domain.AreasAcademicas;
using Sgpla.Modules.Institucional.Domain.EntidadesAcademicas;

namespace Sgpla.Modules.Institucional.Infrastructure.Contratos;

internal sealed class AmbitosInstitucionales(SgplaDbContext contexto) : IAmbitosInstitucionales
{
    public Task<bool> AreaAcademicaActivaAsync(int areaAcademicaId, CancellationToken cancellationToken) =>
        contexto.Set<AreaAcademica>().AnyAsync(a => a.Id == areaAcademicaId, cancellationToken);

    public Task<bool> EntidadAcademicaActivaAsync(int entidadAcademicaId, CancellationToken cancellationToken) =>
        contexto.Set<EntidadAcademica>().AnyAsync(e => e.Id == entidadAcademicaId, cancellationToken);

    public async Task<IReadOnlyDictionary<int, AreaAcademicaResumen>> ObtenerAreasAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<int, AreaAcademicaResumen>();
        }

        return await contexto.Set<AreaAcademica>()
            .IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
            .AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .Select(a => new AreaAcademicaResumen(a.Id, a.Clave, a.Nombre))
            .ToDictionaryAsync(resumen => resumen.Id, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, EntidadAcademicaResumen>> ObtenerEntidadesAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<int, EntidadAcademicaResumen>();
        }

        return await contexto.Set<EntidadAcademica>()
            .IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
            .AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .Select(e => new EntidadAcademicaResumen(e.Id, e.Clave, e.Nombre, e.AreaAcademicaId))
            .ToDictionaryAsync(resumen => resumen.Id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<int>> ObtenerEntidadesDeAreaAsync(
        int areaAcademicaId,
        CancellationToken cancellationToken) =>
        await contexto.Set<EntidadAcademica>()
            .IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
            .AsNoTracking()
            .Where(e => e.AreaAcademicaId == areaAcademicaId)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
}
