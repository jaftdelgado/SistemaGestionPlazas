using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Institucional.Application.AreasAcademicas;
using Sgpla.Modules.Institucional.Domain.AreasAcademicas;

namespace Sgpla.Modules.Institucional.Infrastructure.AreasAcademicas;

internal sealed class AreaAcademicaRepository(SgplaDbContext contexto) : IAreaAcademicaRepository
{
    public Task<AreaAcademica?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        contexto.Set<AreaAcademica>().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> ExisteClaveAsync(int clave, CancellationToken cancellationToken) =>
        contexto.Set<AreaAcademica>()
            .IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
            .AnyAsync(a => a.Clave == clave, cancellationToken);

    // PR 4: consultar entidades activas.
    public Task<bool> TieneEntidadesActivasAsync(int areaAcademicaId, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public void Agregar(AreaAcademica areaAcademica) => contexto.Set<AreaAcademica>().Add(areaAcademica);
}
