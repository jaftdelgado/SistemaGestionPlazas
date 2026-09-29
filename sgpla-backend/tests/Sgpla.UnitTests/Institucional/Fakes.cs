using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.Institucional.Application.AreasAcademicas;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.Institucional.Application.EntidadesAcademicas;
using Sgpla.Modules.Institucional.Domain.AreasAcademicas;
using Sgpla.Modules.Institucional.Domain.EntidadesAcademicas;

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

/// <summary>Repositorio en memoria de entidades académicas. La unicidad se simula con la clave.</summary>
internal sealed class EntidadAcademicaRepositoryFalso : IEntidadAcademicaRepository
{
    private readonly Dictionary<int, EntidadAcademica> _porId = [];

    public List<EntidadAcademica> Agregados { get; } = [];

    public HashSet<string> ClavesExistentes { get; } = new(StringComparer.Ordinal);

    public HashSet<int> CamposExistentes { get; } = [];

    public HashSet<int> AreasAcademicasActivas { get; } = [];

    public void Registrar(int id, EntidadAcademica entidadAcademica) => _porId[id] = entidadAcademica;

    public Task<EntidadAcademica?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(_porId.GetValueOrDefault(id));

    public Task<bool> ExisteClaveAsync(string clave, CancellationToken cancellationToken) =>
        Task.FromResult(ClavesExistentes.Contains(clave));

    public Task<bool> ExisteCampusAsync(int campusId, CancellationToken cancellationToken) =>
        Task.FromResult(CamposExistentes.Contains(campusId));

    public Task<bool> ExisteAreaAcademicaActivaAsync(int areaAcademicaId, CancellationToken cancellationToken) =>
        Task.FromResult(AreasAcademicasActivas.Contains(areaAcademicaId));

    public void Agregar(EntidadAcademica entidadAcademica) => Agregados.Add(entidadAcademica);
}

/// <summary>Responde si el área tiene usuarios DGAA activos, según <see cref="TieneUsuariosActivos"/>.</summary>
internal sealed class UsuariosDeAreaAcademicaFalso : IUsuariosDeAreaAcademica
{
    public bool TieneUsuariosActivos { get; set; }

    public Task<bool> TieneUsuariosActivosAsync(int areaAcademicaId, CancellationToken cancellationToken) =>
        Task.FromResult(TieneUsuariosActivos);
}

/// <summary>Responde si la entidad tiene usuarios activos, según <see cref="TieneUsuariosActivos"/>.</summary>
internal sealed class UsuariosDeEntidadAcademicaFalso : IUsuariosDeEntidadAcademica
{
    public bool TieneUsuariosActivos { get; set; }

    public Task<bool> TieneUsuariosActivosAsync(int entidadAcademicaId, CancellationToken cancellationToken) =>
        Task.FromResult(TieneUsuariosActivos);
}

/// <summary>
/// Responde según <see cref="TieneProgramasActivos"/> y <see cref="TieneProgramas"/> (este último incluye los dados
/// de baja) y cuenta las consultas de cada método.
/// </summary>
internal sealed class ProgramasDeEntidadAcademicaFalso : IProgramasDeEntidadAcademica
{
    public bool TieneProgramasActivos { get; set; }

    public bool TieneProgramas { get; set; }

    public int ConsultasDeActivos { get; private set; }

    public int ConsultasDeTodos { get; private set; }

    public Task<bool> TieneProgramasActivosAsync(int entidadAcademicaId, CancellationToken cancellationToken)
    {
        ConsultasDeActivos++;
        return Task.FromResult(TieneProgramasActivos);
    }

    public Task<bool> TieneProgramasAsync(int entidadAcademicaId, CancellationToken cancellationToken)
    {
        ConsultasDeTodos++;
        return Task.FromResult(TieneProgramas);
    }
}

/// <summary>Municipios en memoria: los ids en <see cref="Existentes"/> existen y tienen el nombre indicado.</summary>
internal sealed class MunicipiosFalso : IMunicipios
{
    public Dictionary<int, string> Existentes { get; } = [];

    public Task<bool> ExisteAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(Existentes.ContainsKey(id));

    public Task<IReadOnlyDictionary<int, string>> ObtenerNombresAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<int, string>>(Existentes
            .Where(par => ids.Contains(par.Key))
            .ToDictionary(par => par.Key, par => par.Value));
}
