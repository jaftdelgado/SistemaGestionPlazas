using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Institucional.Application.EntidadesAcademicas;
using Sgpla.Modules.Institucional.Domain.AreasAcademicas;
using Sgpla.Modules.Institucional.Domain.EntidadesAcademicas;
using Sgpla.Modules.Institucional.Domain.Ubicaciones;

namespace Sgpla.Modules.Institucional.Infrastructure.EntidadesAcademicas;

internal sealed class EntidadAcademicaRepository(SgplaDbContext contexto) : IEntidadAcademicaRepository
{
    public Task<EntidadAcademica?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        contexto.Set<EntidadAcademica>().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<bool> ExisteClaveAsync(string clave, CancellationToken cancellationToken) =>
        contexto.Set<EntidadAcademica>()
            .IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
            .AnyAsync(e => e.Clave == clave, cancellationToken);

    public Task<bool> ExisteCampusAsync(int campusId, CancellationToken cancellationToken) =>
        contexto.Set<Campus>().AnyAsync(c => c.Id == campusId, cancellationToken);

    public Task<bool> ExisteAreaAcademicaActivaAsync(int areaAcademicaId, CancellationToken cancellationToken) =>
        contexto.Set<AreaAcademica>().AnyAsync(a => a.Id == areaAcademicaId, cancellationToken);

    public void Agregar(EntidadAcademica entidadAcademica) => contexto.Set<EntidadAcademica>().Add(entidadAcademica);
}
