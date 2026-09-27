using Sgpla.Modules.Catalogos.Domain.Articulos;

namespace Sgpla.Modules.Catalogos.Application.Articulos;

internal sealed record ArticuloResponse(int Id, string Numero, string Descripcion)
{
    public static ArticuloResponse Desde(Articulo articulo) => new(articulo.Id, articulo.Numero, articulo.Descripcion);
}

/// <summary>Todos los artículos, en orden alfabético del número (con el id como desempate).</summary>
internal sealed record ListarArticulosQuery;

internal sealed record ObtenerArticuloQuery(int Id);
