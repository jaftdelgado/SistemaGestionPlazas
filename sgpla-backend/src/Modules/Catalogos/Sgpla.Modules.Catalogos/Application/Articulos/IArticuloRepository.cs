using Sgpla.Modules.Catalogos.Domain.Articulos;

namespace Sgpla.Modules.Catalogos.Application.Articulos;

internal interface IArticuloRepository
{
    Task<Articulo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Compara el número ya normalizado, en la base.</summary>
    Task<bool> ExisteNumeroAsync(string numero, int? excluirId, CancellationToken cancellationToken);

    void Agregar(Articulo articulo);
}
