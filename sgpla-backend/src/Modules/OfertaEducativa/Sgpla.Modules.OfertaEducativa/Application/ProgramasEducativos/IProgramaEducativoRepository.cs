using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;

namespace Sgpla.Modules.OfertaEducativa.Application.ProgramasEducativos;

internal interface IProgramaEducativoRepository
{
    /// <summary>Solo activos.</summary>
    Task<ProgramaEducativo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Misma entidad, mismo sistema y nombre equivalente (la columna ignora mayúsculas y acentos), incluidos los dados de
    /// baja. <paramref name="excluirId"/> omite al propio programa al modificar.
    /// </summary>
    Task<bool> ExisteNombreAsync(
        int entidadAcademicaId,
        string nombre,
        int sistemaEducativoId,
        int? excluirId,
        CancellationToken cancellationToken);

    /// <summary>Cualquier plan, incluidos los dados de baja.</summary>
    Task<bool> TuvoPlanesAsync(int programaEducativoId, CancellationToken cancellationToken);

    Task<bool> TienePlanesActivosAsync(int programaEducativoId, CancellationToken cancellationToken);

    void Agregar(ProgramaEducativo programaEducativo);
}
