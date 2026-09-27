using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Institucional.Application.AreasAcademicas;
using Sgpla.Modules.Institucional.Domain.AreasAcademicas;
using Sgpla.Modules.Institucional.Domain.EntidadesAcademicas;

namespace Sgpla.Modules.Institucional.Infrastructure.AreasAcademicas;

internal sealed class AreaAcademicaRepository(SgplaDbContext contexto) : IAreaAcademicaRepository
{
    public Task<AreaAcademica?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        contexto.Set<AreaAcademica>().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> ExisteClaveAsync(int clave, CancellationToken cancellationToken) =>
        contexto.Set<AreaAcademica>()
            .IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
            .AnyAsync(a => a.Clave == clave, cancellationToken);

    public Task<bool> TieneEntidadesActivasAsync(int areaAcademicaId, CancellationToken cancellationToken) =>
        contexto.Set<EntidadAcademica>().AnyAsync(e => e.AreaAcademicaId == areaAcademicaId, cancellationToken);

    public void Agregar(AreaAcademica areaAcademica) => contexto.Set<AreaAcademica>().Add(areaAcademica);
}
