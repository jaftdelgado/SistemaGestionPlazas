using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Domain.CatalogosFijos;

internal static class CatalogoFijoErrors
{
    /// <summary>Código <c>&lt;Catalogo&gt;.NoEncontrado</c>, con el nombre de la clase: <c>TipoPlaza.NoEncontrado</c>.</summary>
    public static Error NoEncontrado<TCatalogo>(int id)
        where TCatalogo : CatalogoFijo =>
        Error.NotFound($"{typeof(TCatalogo).Name}.NoEncontrado", $"No existe el valor {id} en el catálogo.");
}
