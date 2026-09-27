namespace Sgpla.Modules.Catalogos.Application.CatalogosFijos;

/// <summary>Valor de un catálogo fijo que solo tiene nombre.</summary>
internal sealed record CatalogoFijoResponse(int Id, string Nombre);

/// <summary>Todos los valores del catálogo <typeparamref name="TCatalogo"/>, en orden de id.</summary>
internal sealed record ListarCatalogoFijoQuery<TCatalogo>;

internal sealed record ObtenerCatalogoFijoQuery<TCatalogo>(int Id);
