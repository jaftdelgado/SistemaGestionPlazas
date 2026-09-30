using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Application.Ambito;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Application.ExperienciasEducativas;
using Sgpla.Modules.OfertaEducativa.Application.PeriodosEscolares;
using Sgpla.Modules.OfertaEducativa.Application.PlanesEstudio;
using Sgpla.Modules.OfertaEducativa.Application.ProgramasEducativos;
using Sgpla.Modules.OfertaEducativa.Domain.PeriodosEscolares;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;
using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;
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

/// <summary>Repositorio en memoria de periodos escolares. La unicidad se simula con la clave normalizada.</summary>
internal sealed class PeriodoEscolarRepositoryFalso : IPeriodoEscolarRepository
{
    private readonly Dictionary<int, PeriodoEscolar> _porId = [];

    public List<PeriodoEscolar> Agregados { get; } = [];

    public HashSet<string> ClavesExistentes { get; } = new(StringComparer.Ordinal);

    public bool TieneProgramaciones { get; set; }

    public void Registrar(int id, PeriodoEscolar periodoEscolar) => _porId[id] = periodoEscolar;

    public Task<PeriodoEscolar?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(_porId.GetValueOrDefault(id));

    public Task<bool> ExisteClaveAsync(string clave, CancellationToken cancellationToken) =>
        Task.FromResult(ClavesExistentes.Contains(clave));

    public Task<bool> TieneProgramacionesAsync(int periodoEscolarId, CancellationToken cancellationToken) =>
        Task.FromResult(TieneProgramaciones);

    public void Agregar(PeriodoEscolar periodoEscolar) => Agregados.Add(periodoEscolar);
}

/// <summary>Responde según <paramref name="tieneReferencias"/> y cuenta las consultas recibidas.</summary>
internal sealed class ReferenciasPeriodoEscolarFalsas(bool tieneReferencias) : IReferenciasPeriodoEscolar
{
    public int Consultas { get; private set; }

    public Task<bool> TieneReferenciasAsync(int periodoEscolarId, CancellationToken cancellationToken)
    {
        Consultas++;
        return Task.FromResult(tieneReferencias);
    }
}

/// <summary>El DGAA solo puede escribir en las entidades de <see cref="EntidadesEscribibles"/>.</summary>
internal sealed class AmbitoOfertaEducativaFalso : IAmbitoOfertaEducativa
{
    public HashSet<int> EntidadesEscribibles { get; } = [];

    public IReadOnlyCollection<int>? EntidadesVisibles { get; set; }

    public Task<IReadOnlyCollection<int>?> EntidadesVisiblesAsync(CancellationToken cancellationToken) =>
        Task.FromResult(EntidadesVisibles);

    public Task<bool> PuedeEscribirEnEntidadAsync(int entidadAcademicaId, CancellationToken cancellationToken) =>
        Task.FromResult(EntidadesEscribibles.Contains(entidadAcademicaId));
}

/// <summary>Clasificaciones en memoria: los ids de cada conjunto existen; los demás no.</summary>
internal sealed class ClasificacionesAcademicasFalso : IClasificacionesAcademicas
{
    public HashSet<int> Sistemas { get; } = [];

    public HashSet<int> Niveles { get; } = [];

    public HashSet<int> Areas { get; } = [];

    /// <summary>Cuántas veces se consultaron las áreas de formación.</summary>
    public int ConsultasDeAreas { get; private set; }

    public Task<IReadOnlyDictionary<int, SistemaEducativoResumen>> ObtenerSistemasEducativosAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<int, SistemaEducativoResumen>>(ids
            .Where(Sistemas.Contains)
            .Distinct()
            .ToDictionary(id => id, id => new SistemaEducativoResumen(id, $"Sistema {id}")));

    public Task<IReadOnlyDictionary<int, NivelFormacionResumen>> ObtenerNivelesFormacionAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<int, NivelFormacionResumen>>(ids
            .Where(Niveles.Contains)
            .Distinct()
            .ToDictionary(id => id, id => new NivelFormacionResumen(id, $"N{id}", $"Nivel {id}")));

    public Task<IReadOnlyDictionary<int, AreaFormacionResumen>> ObtenerAreasFormacionAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        ConsultasDeAreas++;
        return Task.FromResult<IReadOnlyDictionary<int, AreaFormacionResumen>>(ids
            .Where(Areas.Contains)
            .Distinct()
            .ToDictionary(id => id, id => new AreaFormacionResumen(id, $"A{id}", $"Área {id}")));
    }
}

/// <summary>
/// Repositorio en memoria de programas educativos. La unicidad se simula con los nombres registrados, comparados de
/// forma ordinal, y el propio programa se excluye por su id, como hace la consulta real.
/// </summary>
internal sealed class ProgramaEducativoRepositoryFalso : IProgramaEducativoRepository
{
    private readonly Dictionary<int, ProgramaEducativo> _porId = [];
    private readonly List<(int EntidadAcademicaId, string Nombre, int SistemaEducativoId, int Id)> _nombres = [];

    public List<ProgramaEducativo> Agregados { get; } = [];

    public bool TuvoPlanes { get; set; }

    public bool TienePlanesActivos { get; set; }

    public void Registrar(int id, ProgramaEducativo programa)
    {
        _porId[id] = programa;
        _nombres.Add((programa.EntidadAcademicaId, programa.Nombre, programa.SistemaEducativoId, id));
    }

    public void RegistrarNombre(int entidadAcademicaId, string nombre, int sistemaEducativoId, int id) =>
        _nombres.Add((entidadAcademicaId, nombre, sistemaEducativoId, id));

    public Task<ProgramaEducativo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(_porId.GetValueOrDefault(id));

    public Task<bool> ExisteNombreAsync(
        int entidadAcademicaId, string nombre, int sistemaEducativoId, int? excluirId, CancellationToken cancellationToken) =>
        Task.FromResult(_nombres.Any(n =>
            n.EntidadAcademicaId == entidadAcademicaId
            && n.SistemaEducativoId == sistemaEducativoId
            && string.Equals(n.Nombre, nombre, StringComparison.Ordinal)
            && (excluirId is null || n.Id != excluirId)));

    public Task<bool> TuvoPlanesAsync(int programaEducativoId, CancellationToken cancellationToken) =>
        Task.FromResult(TuvoPlanes);

    public Task<bool> TienePlanesActivosAsync(int programaEducativoId, CancellationToken cancellationToken) =>
        Task.FromResult(TienePlanesActivos);

    public void Agregar(ProgramaEducativo programaEducativo) => Agregados.Add(programaEducativo);
}

/// <summary>
/// Repositorio en memoria de planes de estudio. Las entidades no tienen id asignado (no pasan por la base), así que
/// <see cref="EntidadDelPlan"/> es la entidad de todos los planes registrados; la unicidad del código se simula con los
/// pares programa-código registrados.
/// </summary>
internal sealed class PlanEstudiosRepositoryFalso : IPlanEstudiosRepository
{
    private readonly Dictionary<int, PlanEstudios> _porId = [];

    public List<PlanEstudios> Agregados { get; } = [];

    /// <summary>Programa activo → entidad académica. Un programa ausente no existe o está dado de baja.</summary>
    public Dictionary<int, int> EntidadesDeProgramasActivos { get; } = [];

    public HashSet<(int ProgramaEducativoId, string Codigo)> CodigosExistentes { get; } = [];

    public int EntidadDelPlan { get; set; }

    public bool TieneExperienciasConProgramacionesActivas { get; set; }

    public void Registrar(int id, PlanEstudios plan) => _porId[id] = plan;

    public Task<PlanEstudios?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(_porId.GetValueOrDefault(id));

    public Task<PlanEstudios?> ObtenerConExperienciasAsync(int id, CancellationToken cancellationToken) =>
        ObtenerPorIdAsync(id, cancellationToken);

    public Task<bool> ExisteCodigoAsync(int programaEducativoId, string codigo, CancellationToken cancellationToken) =>
        Task.FromResult(CodigosExistentes.Contains((programaEducativoId, codigo)));

    public Task<int?> ObtenerEntidadDeProgramaActivoAsync(int programaEducativoId, CancellationToken cancellationToken) =>
        Task.FromResult<int?>(EntidadesDeProgramasActivos.TryGetValue(programaEducativoId, out var entidadId) ? entidadId : null);

    public Task<int> ObtenerEntidadDelPlanAsync(int planEstudiosId, CancellationToken cancellationToken) =>
        Task.FromResult(EntidadDelPlan);

    public Task<bool> TieneExperienciasConProgramacionesActivasAsync(int planEstudiosId, CancellationToken cancellationToken) =>
        Task.FromResult(TieneExperienciasConProgramacionesActivas);

    public void Agregar(PlanEstudios planEstudios) => Agregados.Add(planEstudios);
}

/// <summary>Responde según <paramref name="tieneReferencias"/> y guarda los ids de las EE que recibe.</summary>
internal sealed class ReferenciasExperienciaEducativaFalsas(bool tieneReferencias) : IReferenciasExperienciaEducativa
{
    public List<IReadOnlyCollection<int>> Consultas { get; } = [];

    public Task<bool> TieneReferenciasAsync(
        IReadOnlyCollection<int> experienciaEducativaIds,
        CancellationToken cancellationToken)
    {
        Consultas.Add(experienciaEducativaIds);
        return Task.FromResult(tieneReferencias);
    }
}

/// <summary>
/// Repositorio en memoria de experiencias educativas. Las entidades no tienen id asignado, así que
/// <see cref="EntidadDeLaExperiencia"/> es la entidad de todas las registradas; la unicidad de materia y curso se simula
/// con los pares registrados, comparados de forma ordinal.
/// </summary>
internal sealed class ExperienciaEducativaRepositoryFalso : IExperienciaEducativaRepository
{
    private readonly Dictionary<int, ExperienciaEducativa> _porId = [];

    public HashSet<(string Materia, string Curso)> MateriasYCursosExistentes { get; } = [];

    public int EntidadDeLaExperiencia { get; set; }

    public bool TuvoProgramaciones { get; set; }

    public bool TieneProgramacionesActivas { get; set; }

    public void Registrar(int id, ExperienciaEducativa experiencia) => _porId[id] = experiencia;

    public Task<ExperienciaEducativa?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(_porId.GetValueOrDefault(id));

    public Task<bool> ExisteMateriaCursoAsync(
        int planEstudiosId, string materia, string curso, CancellationToken cancellationToken) =>
        Task.FromResult(MateriasYCursosExistentes.Contains((materia, curso)));

    public Task<bool> TuvoProgramacionesAsync(int experienciaEducativaId, CancellationToken cancellationToken) =>
        Task.FromResult(TuvoProgramaciones);

    public Task<bool> TieneProgramacionesActivasAsync(int experienciaEducativaId, CancellationToken cancellationToken) =>
        Task.FromResult(TieneProgramacionesActivas);

    public Task<int> ObtenerEntidadAsync(int experienciaEducativaId, CancellationToken cancellationToken) =>
        Task.FromResult(EntidadDeLaExperiencia);
}
