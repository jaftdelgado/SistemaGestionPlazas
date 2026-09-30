using Sgpla.SharedKernel;

namespace Sgpla.Modules.Usuarios.Domain.Cuentas;

/// <summary>Política de contraseñas locales de Superusuario. No normaliza: los espacios cuentan.</summary>
internal static class PoliticaContrasena
{
    public const int LongitudMinima = 8;
    public const int LongitudMaxima = 128;

    public static Result Validar(string contrasena)
    {
        ArgumentNullException.ThrowIfNull(contrasena);

        if (contrasena.Length < LongitudMinima || contrasena.Length > LongitudMaxima)
        {
            return UsuarioErrors.ContrasenaLongitudInvalida;
        }

        var tieneMayuscula = contrasena.Any(char.IsUpper);
        var tieneMinuscula = contrasena.Any(char.IsLower);
        var tieneDigito = contrasena.Any(caracter => caracter is >= '0' and <= '9');
        var tieneSimbolo = contrasena.Any(caracter => !char.IsLetterOrDigit(caracter) && !char.IsWhiteSpace(caracter));

        if (!tieneMayuscula || !tieneMinuscula || !tieneDigito || !tieneSimbolo)
        {
            return UsuarioErrors.ContrasenaDebil;
        }

        return Result.Success();
    }
}
