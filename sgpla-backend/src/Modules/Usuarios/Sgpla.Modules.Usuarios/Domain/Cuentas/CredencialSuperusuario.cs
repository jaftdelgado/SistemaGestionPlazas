namespace Sgpla.Modules.Usuarios.Domain.Cuentas;

/// <summary>Credencial 1:1 del agregado <see cref="Usuario"/> exclusiva del rol <c>Superusuario</c> (DATABASE.md §6.20).</summary>
internal sealed class CredencialSuperusuario
{
    public const int LongitudMaximaContrasena = 500;

    private CredencialSuperusuario()
    {
    }

    /// <summary>Cadena PHC completa de Argon2id; nunca la contraseña original.</summary>
    public string Contrasena { get; private set; } = string.Empty;

    /// <summary><c>null</c> significa una contraseña temporal pendiente de cambio.</summary>
    public DateTime? FechaActualizacion { get; private set; }

    public DateTime? FechaEliminacion { get; private set; }

    public static CredencialSuperusuario Crear(string verificadorTemporal) =>
        new() { Contrasena = verificadorTemporal, FechaActualizacion = null };

    /// <summary>Cambio hecho por el propio Superusuario.</summary>
    public void EstablecerContrasena(string verificador, DateTime utc)
    {
        Contrasena = verificador;
        FechaActualizacion = utc;
    }

    /// <summary>Cambio hecho por otro Superusuario: regresa a pendiente de cambio.</summary>
    public void Restablecer(string verificadorTemporal)
    {
        Contrasena = verificadorTemporal;
        FechaActualizacion = null;
    }

    /// <summary>Rehash tras un acceso exitoso: conserva <see cref="FechaActualizacion"/>.</summary>
    public void ActualizarVerificador(string verificador) => Contrasena = verificador;

    /// <summary>Idempotente: si ya tiene fecha, no la reemplaza.</summary>
    public void DarDeBaja(DateTime utc) => FechaEliminacion ??= utc;
}
