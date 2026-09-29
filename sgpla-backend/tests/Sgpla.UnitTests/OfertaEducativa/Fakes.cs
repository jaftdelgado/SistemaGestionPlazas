using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.SharedKernel;

namespace Sgpla.UnitTests.OfertaEducativa;

/// <summary>Usuario en curso con el rol y el ámbito indicados.</summary>
internal sealed class UsuarioActualFalso(Rol rol, int? areaAcademicaId = null, int? entidadAcademicaId = null) : ICurrentUser
{
    public int Id => 1;

    public Rol Rol => rol;

    public int? AreaAcademicaId => areaAcademicaId;

    public int? EntidadAcademicaId => entidadAcademicaId;
}

/// <summary>Entidades en memoria: <see cref="ObtenerEntidadesAsync"/> y <see cref="ObtenerEntidadesDeAreaAsync"/> incluyen las dadas de baja.</summary>
internal sealed class AmbitosDeEntidadesFalso : IAmbitosInstitucionales
{
    private readonly Dictionary<int, (int AreaAcademicaId, bool Activa)> _entidades = [];

    public void Registrar(int entidadAcademicaId, int areaAcademicaId, bool activa = true) =>
        _entidades[entidadAcademicaId] = (areaAcademicaId, activa);

    public Task<bool> AreaAcademicaActivaAsync(int areaAcademicaId, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task<bool> EntidadAcademicaActivaAsync(int entidadAcademicaId, CancellationToken cancellationToken) =>
        Task.FromResult(_entidades.TryGetValue(entidadAcademicaId, out var entidad) && entidad.Activa);

    public Task<IReadOnlyDictionary<int, AreaAcademicaResumen>> ObtenerAreasAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<int, AreaAcademicaResumen>>(new Dictionary<int, AreaAcademicaResumen>());

    public Task<IReadOnlyDictionary<int, EntidadAcademicaResumen>> ObtenerEntidadesAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<int, EntidadAcademicaResumen>>(_entidades
            .Where(par => ids.Contains(par.Key))
            .ToDictionary(par => par.Key, par => new EntidadAcademicaResumen(par.Key, $"E{par.Key}", $"Entidad {par.Key}", par.Value.AreaAcademicaId)));

    public Task<IReadOnlyCollection<int>> ObtenerEntidadesDeAreaAsync(int areaAcademicaId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<int>>(_entidades
            .Where(par => par.Value.AreaAcademicaId == areaAcademicaId)
            .Select(par => par.Key)
            .ToList());
}
