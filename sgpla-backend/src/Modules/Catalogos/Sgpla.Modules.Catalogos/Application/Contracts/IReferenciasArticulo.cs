namespace Sgpla.Modules.Catalogos.Application.Contracts;

/// <summary>
/// Lo implementa cada módulo que guarda referencias a un artículo (Publicacion, por sus Avisos). Con una referencia,
/// el número del artículo queda inmutable.
/// </summary>
public interface IReferenciasArticulo
{
    Task<bool> TieneReferenciasAsync(int articuloId, CancellationToken cancellationToken);
}
