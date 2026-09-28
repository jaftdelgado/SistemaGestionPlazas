using Sgpla.SharedKernel;

namespace Sgpla.BuildingBlocks.Application;

/// <summary>Usuario de la petición en curso, ya verificado contra la base (Modulo_Usuarios.md, sección 6).</summary>
public interface ICurrentUser
{
    int Id { get; }

    Rol Rol { get; }

    /// <summary>Solo para <see cref="Rol.Dgaa"/>.</summary>
    int? AreaAcademicaId { get; }

    /// <summary>Solo para <see cref="Rol.EntidadAcademica"/>.</summary>
    int? EntidadAcademicaId { get; }
}

/// <summary>Nombres de las políticas de autorización. Se usan con <c>RequireAuthorization</c>.</summary>
public static class Politicas
{
    /// <summary>Cualquier sesión válida, aunque tenga la contraseña pendiente de cambio.</summary>
    public const string SesionIniciada = nameof(SesionIniciada);

    /// <summary>Sesión válida y sin contraseña pendiente de cambio.</summary>
    public const string Autenticado = nameof(Autenticado);

    /// <summary><see cref="Autenticado"/> con el rol Superusuario.</summary>
    public const string Superusuario = nameof(Superusuario);
}
