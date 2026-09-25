using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;

namespace Sgpla.BuildingBlocks.Infrastructure.Persistence;

public static class PaginacionExtensions
{
    /// <summary>
    /// Ejecuta la consulta ya ordenada y devuelve la página solicitada. La consulta debe tener un orden total
    /// (con el <c>Id</c> como desempate) para que la paginación sea estable.
    /// </summary>
    public static async Task<Pagina<T>> PaginarAsync<T>(
        this IQueryable<T> consulta,
        Paginacion paginacion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paginacion);

        var total = await consulta.CountAsync(cancellationToken);

        // En long para que una página muy lejana no desborde el cálculo: simplemente queda vacía.
        var omitidos = (long)(paginacion.Pagina - 1) * paginacion.TamanoPagina;
        if (omitidos >= total)
        {
            return new Pagina<T>([], paginacion.Pagina, paginacion.TamanoPagina, total);
        }

        var elementos = await consulta
            .Skip((int)omitidos)
            .Take(paginacion.TamanoPagina)
            .ToListAsync(cancellationToken);

        return new Pagina<T>(elementos, paginacion.Pagina, paginacion.TamanoPagina, total);
    }
}
