namespace Sgpla.Modules.Catalogos.Application.CatalogosFijos;

/// <summary>Respuesta de los catálogos con clave (nivel y área de formación).</summary>
internal sealed record ClasificacionConClaveResponse(int Id, string Clave, string Nombre);

/// <summary>Todos los niveles de formación, en orden de id.</summary>
internal sealed record ListarNivelesFormacionQuery;

internal sealed record ObtenerNivelFormacionQuery(int Id);
