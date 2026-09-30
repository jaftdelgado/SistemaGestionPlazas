using Sgpla.SharedKernel;

namespace Sgpla.Modules.Institucional.Domain.AreasAcademicas;

/// <summary>Área académica de la UV. Catálogo global administrable, con baja lógica y sin restauración.</summary>
internal sealed class AreaAcademica : Entity, IEliminable
{
    public const int LongitudMaximaNombre = 200;
    public const int LongitudTelefono = 10;
    public const int LongitudMaximaExtension = 10;

    private AreaAcademica()
    {
    }

    public int Clave { get; private set; }

    public string Nombre { get; private set; } = string.Empty;

    public string Telefono { get; private set; } = string.Empty;

    public string? Extension { get; private set; }

    public DateTime? FechaEliminacion { get; private set; }

    public static Result<AreaAcademica> Crear(int clave, string nombre, string telefono, string? extension)
    {
        if (clave <= 0)
        {
            return AreaAcademicaErrors.ClaveNoPositiva;
        }

        var nombreNormalizado = NormalizarNombre(nombre);
        if (nombreNormalizado.IsFailure)
        {
            return nombreNormalizado.Error;
        }

        var telefonoNormalizado = NormalizarTelefono(telefono);
        if (telefonoNormalizado.IsFailure)
        {
            return telefonoNormalizado.Error;
        }

        var extensionNormalizada = NormalizarExtension(extension);
        if (extensionNormalizada.IsFailure)
        {
            return extensionNormalizada.Error;
        }

        return new AreaAcademica
        {
            Clave = clave,
            Nombre = nombreNormalizado.Value,
            Telefono = telefonoNormalizado.Value,
            Extension = extensionNormalizada.Value,
        };
    }

    /// <summary>Reemplazo completo: una <paramref name="extension"/> <c>null</c> o vacía quita la extensión.</summary>
    public Result Modificar(string nombre, string telefono, string? extension)
    {
        var nombreNormalizado = NormalizarNombre(nombre);
        if (nombreNormalizado.IsFailure)
        {
            return nombreNormalizado.Error;
        }

        var telefonoNormalizado = NormalizarTelefono(telefono);
        if (telefonoNormalizado.IsFailure)
        {
            return telefonoNormalizado.Error;
        }

        var extensionNormalizada = NormalizarExtension(extension);
        if (extensionNormalizada.IsFailure)
        {
            return extensionNormalizada.Error;
        }

        Nombre = nombreNormalizado.Value;
        Telefono = telefonoNormalizado.Value;
        Extension = extensionNormalizada.Value;
        return Result.Success();
    }

    /// <summary>Idempotente: si ya tiene fecha, no la reemplaza.</summary>
    public void DarDeBaja(DateTime utc) => FechaEliminacion ??= utc;

    private static Result<string> NormalizarNombre(string? nombre)
    {
        var normalizado = Normalizacion.Texto(nombre);

        if (normalizado.Length == 0)
        {
            return AreaAcademicaErrors.NombreVacio;
        }

        if (normalizado.Length > LongitudMaximaNombre)
        {
            return AreaAcademicaErrors.NombreDemasiadoLargo;
        }

        return normalizado;
    }

    private static Result<string> NormalizarTelefono(string? telefono)
    {
        var recortado = Normalizacion.Recortar(telefono);

        if (recortado.Length == 0)
        {
            return AreaAcademicaErrors.TelefonoVacio;
        }

        if (recortado.Length != LongitudTelefono || !Normalizacion.SonDigitos(recortado))
        {
            return AreaAcademicaErrors.TelefonoFormatoInvalido;
        }

        return recortado;
    }

    private static Result<string?> NormalizarExtension(string? extension)
    {
        var recortada = Normalizacion.Recortar(extension);

        if (recortada.Length == 0)
        {
            return Result.Success<string?>(null);
        }

        if (recortada.Length > LongitudMaximaExtension || !Normalizacion.SonDigitos(recortada))
        {
            return AreaAcademicaErrors.ExtensionFormatoInvalido;
        }

        return Result.Success<string?>(recortada);
    }
}
