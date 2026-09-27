namespace Sgpla.Modules.Catalogos.Application.CatalogosFijos;

internal sealed record ModalidadRecepcionResponse(int Id, string Nombre, bool RequiereLugar);

/// <summary>Todas las modalidades, en orden de id.</summary>
internal sealed record ListarModalidadesRecepcionQuery;

internal sealed record ObtenerModalidadRecepcionQuery(int Id);
