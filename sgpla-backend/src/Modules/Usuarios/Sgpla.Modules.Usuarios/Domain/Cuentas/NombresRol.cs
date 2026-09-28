using Sgpla.SharedKernel;

namespace Sgpla.Modules.Usuarios.Domain.Cuentas;

/// <summary>Nombres de los roles, exactamente como los carga la semilla de <c>usuarios.rol</c> (DATABASE.md §6.16).</summary>
internal static class NombresRol
{
    public static string Nombre(Rol rol) => rol switch
    {
        Rol.Superusuario => "Superusuario",
        Rol.Dgaa => "DGAA",
        Rol.EntidadAcademica => "Entidad Académica",
        _ => throw new ArgumentOutOfRangeException(nameof(rol), rol, "Rol desconocido."),
    };
}
