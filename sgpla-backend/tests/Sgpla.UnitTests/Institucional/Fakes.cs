using Sgpla.Modules.Institucional.Application.AreasAcademicas;
using Sgpla.Modules.Institucional.Domain.AreasAcademicas;

namespace Sgpla.UnitTests.Institucional;

/// <summary>Repositorio en memoria de áreas académicas. La unicidad se simula con la clave.</summary>
internal sealed class AreaAcademicaRepositoryFalso : IAreaAcademicaRepository
{
    private readonly Dictionary<int, AreaAcademica> _porId = [];

    public List<AreaAcademica> Agregados { get; } = [];

    public HashSet<int> ClavesExistentes { get; } = [];

    public bool TieneEntidadesActivas { get; set; }

    public void Registrar(int id, AreaAcademica areaAcademica) => _porId[id] = areaAcademica;

    public Task<AreaAcademica?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(_porId.GetValueOrDefault(id));

    public Task<bool> ExisteClaveAsync(int clave, CancellationToken cancellationToken) =>
        Task.FromResult(ClavesExistentes.Contains(clave));

    public Task<bool> TieneEntidadesActivasAsync(int areaAcademicaId, CancellationToken cancellationToken) =>
        Task.FromResult(TieneEntidadesActivas);

    public void Agregar(AreaAcademica areaAcademica) => Agregados.Add(areaAcademica);
}

/// <summary>Reloj en memoria: siempre devuelve el mismo instante.</summary>
internal sealed class TimeProviderFalso(DateTimeOffset ahora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => ahora;
}
