using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;

namespace Sgpla.Modules.OfertaEducativa.Application.ExperienciasEducativas;

internal interface IExperienciaEducativaRepository
{
    /// <summary>Solo activas.</summary>
    Task<ExperienciaEducativa?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Mismo plan, materia y curso normalizados, incluidas las dadas de baja.</summary>
    Task<bool> ExisteMateriaCursoAsync(int planEstudiosId, string materia, string curso, CancellationToken cancellationToken);

    /// <summary>Cualquier programación de la EE, incluidas las dadas de baja (D13).</summary>
    Task<bool> TuvoProgramacionesAsync(int experienciaEducativaId, CancellationToken cancellationToken);

    Task<bool> TieneProgramacionesActivasAsync(int experienciaEducativaId, CancellationToken cancellationToken);

    /// <summary>Entidad de la EE (por su plan y su programa), para el ámbito.</summary>
    Task<int> ObtenerEntidadAsync(int experienciaEducativaId, CancellationToken cancellationToken);
}
