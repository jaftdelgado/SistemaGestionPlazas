using Sgpla.Modules.Institucional.Domain.EntidadesAcademicas;

namespace Sgpla.Modules.Institucional.Application.EntidadesAcademicas;

internal interface IEntidadAcademicaRepository
{
    /// <summary>Solo activas.</summary>
    Task<EntidadAcademica?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Compara la clave ya normalizada e incluye las dadas de baja.</summary>
    Task<bool> ExisteClaveAsync(string clave, CancellationToken cancellationToken);

    Task<bool> ExisteCampusAsync(int campusId, CancellationToken cancellationToken);

    /// <summary>Solo áreas activas: una dada de baja cuenta como inexistente (D5).</summary>
    Task<bool> ExisteAreaAcademicaActivaAsync(int areaAcademicaId, CancellationToken cancellationToken);

    void Agregar(EntidadAcademica entidadAcademica);
}
