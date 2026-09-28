using Sgpla.SharedKernel;

namespace Sgpla.Modules.Usuarios.Domain.Cuentas;

/// <summary>
/// Identidad de acceso al sistema (DATABASE.md §6.17). El rol decide el mecanismo de autenticación y exactamente
/// qué parte del agregado tiene: un Superusuario su <see cref="Credencial"/>, un DGAA su <see cref="PerfilDgaa"/>
/// y una Entidad Académica su <see cref="PerfilEntidadAcademica"/>. Con baja lógica y sin restauración
/// (Modulo_Usuarios.md, decisión D3).
/// </summary>
internal sealed class Usuario : Entity, IEliminable
{
    public const int LongitudMaximaCorreo = 254;
    public const int LongitudMaximaNombre = 200;

    private Usuario()
    {
    }

    public string Correo { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public Rol Rol { get; private set; }

    public PerfilDgaa? PerfilDgaa { get; private set; }

    public PerfilEntidadAcademica? PerfilEntidadAcademica { get; private set; }

    public CredencialSuperusuario? Credencial { get; private set; }

    public DateTime? FechaEliminacion { get; private set; }

    public int? AreaAcademicaId => PerfilDgaa?.AreaAcademicaId;

    public int? EntidadAcademicaId => PerfilEntidadAcademica?.EntidadAcademicaId;

    public bool CambioContrasenaPendiente => Credencial is { FechaActualizacion: null };

    public static Result<Usuario> CrearSuperusuario(string correo, string nombre, string verificadorTemporal)
    {
        var validado = Validar(correo, nombre, exigeDominioInstitucional: false);
        if (validado.IsFailure)
        {
            return validado.Error;
        }

        return new Usuario
        {
            Correo = validado.Value.Correo,
            Nombre = validado.Value.Nombre,
            Rol = Rol.Superusuario,
            Credencial = CredencialSuperusuario.Crear(verificadorTemporal),
        };
    }

    public static Result<Usuario> CrearDgaa(string correo, string nombre, int areaAcademicaId)
    {
        var validado = Validar(correo, nombre, exigeDominioInstitucional: true);
        if (validado.IsFailure)
        {
            return validado.Error;
        }

        return new Usuario
        {
            Correo = validado.Value.Correo,
            Nombre = validado.Value.Nombre,
            Rol = Rol.Dgaa,
            PerfilDgaa = PerfilDgaa.Crear(areaAcademicaId),
        };
    }

    public static Result<Usuario> CrearEntidadAcademica(string correo, string nombre, int entidadAcademicaId)
    {
        var validado = Validar(correo, nombre, exigeDominioInstitucional: true);
        if (validado.IsFailure)
        {
            return validado.Error;
        }

        return new Usuario
        {
            Correo = validado.Value.Correo,
            Nombre = validado.Value.Nombre,
            Rol = Rol.EntidadAcademica,
            PerfilEntidadAcademica = PerfilEntidadAcademica.Crear(entidadAcademicaId),
        };
    }

    /// <summary>Si falla, la cuenta no cambia.</summary>
    public Result CambiarNombre(string nombre)
    {
        var nombreNormalizado = NormalizarNombre(nombre);
        if (nombreNormalizado.IsFailure)
        {
            return nombreNormalizado.Error;
        }

        Nombre = nombreNormalizado.Value;
        return Result.Success();
    }

    /// <summary>Idempotente. Da de baja también la credencial, con el mismo instante (DATABASE.md §13.2).</summary>
    public void DarDeBaja(DateTime utc)
    {
        FechaEliminacion ??= utc;
        Credencial?.DarDeBaja(utc);
    }

    /// <summary>Cambio hecho por el propio Superusuario.</summary>
    public void EstablecerContrasena(string verificador, DateTime utc)
    {
        ExigirCredencial().EstablecerContrasena(verificador, utc);
    }

    /// <summary>Cambio hecho por otro Superusuario: regresa la cuenta a contraseña pendiente.</summary>
    public void RestablecerContrasena(string verificadorTemporal)
    {
        ExigirCredencial().Restablecer(verificadorTemporal);
    }

    /// <summary>Rehash tras un acceso exitoso.</summary>
    public void ActualizarVerificador(string verificador)
    {
        ExigirCredencial().ActualizarVerificador(verificador);
    }

    private CredencialSuperusuario ExigirCredencial()
    {
        if (Rol != Rol.Superusuario || Credencial is null)
        {
            throw new InvalidOperationException("Solo una cuenta de Superusuario tiene contraseña.");
        }

        return Credencial;
    }

    private static Result<(string Correo, string Nombre)> Validar(string? correo, string? nombre, bool exigeDominioInstitucional)
    {
        var correoNormalizado = NormalizarCorreo(correo, exigeDominioInstitucional);
        if (correoNormalizado.IsFailure)
        {
            return correoNormalizado.Error;
        }

        var nombreNormalizado = NormalizarNombre(nombre);
        if (nombreNormalizado.IsFailure)
        {
            return nombreNormalizado.Error;
        }

        return (correoNormalizado.Value, nombreNormalizado.Value);
    }

    private static Result<string> NormalizarCorreo(string? correo, bool exigeDominioInstitucional)
    {
        var normalizado = Normalizacion.Recortar(correo).ToLowerInvariant();

        if (normalizado.Length == 0)
        {
            return UsuarioErrors.CorreoVacio;
        }

        if (normalizado.Length > LongitudMaximaCorreo)
        {
            return UsuarioErrors.CorreoDemasiadoLargo;
        }

        if (!TieneFormatoValido(normalizado))
        {
            return UsuarioErrors.CorreoFormatoInvalido;
        }

        if (exigeDominioInstitucional && !normalizado.EndsWith("@uv.mx", StringComparison.Ordinal))
        {
            return UsuarioErrors.CorreoNoInstitucional;
        }

        return normalizado;
    }

    /// <summary>Solo ASCII visible, exactamente una '@' con algo antes, y un dominio con un punto entre caracteres.</summary>
    private static bool TieneFormatoValido(string correo)
    {
        if (!correo.All(caracter => caracter is >= '!' and <= '~'))
        {
            return false;
        }

        var arroba = correo.IndexOf('@');
        if (arroba <= 0 || correo.IndexOf('@', arroba + 1) != -1)
        {
            return false;
        }

        var dominio = correo[(arroba + 1)..];
        var punto = dominio.IndexOf('.');
        return punto > 0 && punto < dominio.Length - 1;
    }

    private static Result<string> NormalizarNombre(string? nombre)
    {
        var normalizado = Normalizacion.Texto(nombre);

        if (normalizado.Length == 0)
        {
            return UsuarioErrors.NombreVacio;
        }

        if (normalizado.Length > LongitudMaximaNombre)
        {
            return UsuarioErrors.NombreDemasiadoLargo;
        }

        return normalizado;
    }
}
