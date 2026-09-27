using Sgpla.Modules.Institucional.Domain.AreasAcademicas;

namespace Sgpla.Modules.Institucional.Application.AreasAcademicas;

internal interface IAreaAcademicaRepository
{
    /// <summary>Solo activas (filtro de baja lógica).</summary>
    Task<AreaAcademica?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Incluye las dadas de baja: la clave no se reutiliza.</summary>
    Task<bool> ExisteClaveAsync(int clave, CancellationToken cancellationToken);

    Task<bool> TieneEntidadesActivasAsync(int areaAcademicaId, CancellationToken cancellationToken);

    void Agregar(AreaAcademica areaAcademica);
}
