namespace Sgpla.Modules.Usuarios.Application.Sesion;

internal sealed record RolResponse(byte Id, string Nombre);

internal sealed record AreaAcademicaResumenResponse(int Id, int Clave, string Nombre);

internal sealed record EntidadAcademicaResumenResponse(int Id, string Clave, string Nombre);

internal sealed record UsuarioSesionResponse(
    int Id,
    string Correo,
    string Nombre,
    RolResponse Rol,
    AreaAcademicaResumenResponse? AreaAcademica,
    EntidadAcademicaResumenResponse? EntidadAcademica,
    bool CambioContrasenaPendiente);

internal sealed record SesionResponse(string Token, DateTime ExpiraEn, UsuarioSesionResponse Usuario);

/// <summary>La cuenta activa <paramref name="UsuarioId"/> con su ámbito. 404 si no existe.</summary>
internal sealed record ObtenerUsuarioSesionQuery(int UsuarioId);
