using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;

namespace Sgpla.Modules.OfertaEducativa.Application.PlanesEstudio;

internal interface IPlanEstudiosRepository
{
    /// <summary>Solo activos, sin sus EE.</summary>
    Task<PlanEstudios?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Solo activos, con sus EE activas cargadas (para la baja conjunta).</summary>
    Task<PlanEstudios?> ObtenerConExperienciasAsync(int id, CancellationToken cancellationToken);

    /// <summary>Mismo programa y código normalizado, incluidos los dados de baja.</summary>
    Task<bool> ExisteCodigoAsync(int programaEducativoId, string codigo, CancellationToken cancellationToken);

    /// <summary>Entidad del programa activo, o <c>null</c> si el programa no existe o está dado de baja.</summary>
    Task<int?> ObtenerEntidadDeProgramaActivoAsync(int programaEducativoId, CancellationToken cancellationToken);

    /// <summary>Entidad del plan (por su programa), para el ámbito.</summary>
    Task<int> ObtenerEntidadDelPlanAsync(int planEstudiosId, CancellationToken cancellationToken);

    Task<bool> TieneExperienciasConProgramacionesActivasAsync(int planEstudiosId, CancellationToken cancellationToken);

    void Agregar(PlanEstudios planEstudios);
}
