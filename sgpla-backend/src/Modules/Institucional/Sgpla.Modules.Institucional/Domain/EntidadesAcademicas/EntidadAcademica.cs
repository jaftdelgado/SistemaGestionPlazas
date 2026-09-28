using Sgpla.SharedKernel;

namespace Sgpla.Modules.Institucional.Domain.EntidadesAcademicas;

/// <summary>
/// Unidad académica de un campus (DATABASE.md §6.5), clasificada por un área y localizada en un municipio. Con
/// baja lógica y sin restauración (Modulo_Institucional.md, decisión D3).
/// </summary>
internal sealed class EntidadAcademica : Entity, IEliminable
{
    public const int LongitudMaximaClave = 50;
    public const int LongitudMaximaNombre = 200;
    public const int LongitudMaximaCalle = 200;
    public const int LongitudMaximaNumeroExterior = 20;
    public const int LongitudMaximaColonia = 150;
    public const int LongitudCodigoPostal = 5;
    public const int LongitudTelefono = 10;
    public const int LongitudMaximaExtension = 10;

    private EntidadAcademica()
    {
    }

    public string Clave { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public string Calle { get; private set; } = string.Empty;

    public string? NumeroExterior { get; private set; }

    public string Colonia { get; private set; } = string.Empty;

    public string CodigoPostal { get; private set; } = string.Empty;

    public string Telefono { get; private set; } = string.Empty;

    public string? Extension { get; private set; }

    public int CampusId { get; private set; }

    public int AreaAcademicaId { get; private set; }

    public int MunicipioId { get; private set; }

    public DateTime? FechaEliminacion { get; private set; }

    public static Result<EntidadAcademica> Crear(
        string clave,
        string nombre,
        string calle,
        string? numeroExterior,
        string colonia,
        string codigoPostal,
        string telefono,
        string? extension,
        int campusId,
        int areaAcademicaId,
        int municipioId)
    {
        var validado = Validar(clave, nombre, calle, numeroExterior, colonia, codigoPostal, telefono, extension);
        if (validado.IsFailure)
        {
            return validado.Error;
        }

        var v = validado.Value;
        return new EntidadAcademica
        {
            Clave = v.Clave,
            Nombre = v.Nombre,
            Calle = v.Calle,
            NumeroExterior = v.NumeroExterior,
            Colonia = v.Colonia,
            CodigoPostal = v.CodigoPostal,
            Telefono = v.Telefono,
            Extension = v.Extension,
            CampusId = campusId,
            AreaAcademicaId = areaAcademicaId,
            MunicipioId = municipioId,
        };
    }

    /// <summary>Mismo orden y reglas que <see cref="Crear"/>, sin clave ni campus. Reemplazo completo.</summary>
    public Result Modificar(
        string nombre,
        string calle,
        string? numeroExterior,
        string colonia,
        string codigoPostal,
        string telefono,
        string? extension,
        int areaAcademicaId,
        int municipioId)
    {
        var validado = Validar(Clave, nombre, calle, numeroExterior, colonia, codigoPostal, telefono, extension);
        if (validado.IsFailure)
        {
            return validado.Error;
        }

        var v = validado.Value;
        Nombre = v.Nombre;
        Calle = v.Calle;
        NumeroExterior = v.NumeroExterior;
        Colonia = v.Colonia;
        CodigoPostal = v.CodigoPostal;
        Telefono = v.Telefono;
        Extension = v.Extension;
        AreaAcademicaId = areaAcademicaId;
        MunicipioId = municipioId;
        return Result.Success();
    }

    public void DarDeBaja(DateTime utc) => FechaEliminacion ??= utc;

    private static Result<DatosValidados> Validar(
        string clave,
        string nombre,
        string calle,
        string? numeroExterior,
        string colonia,
        string codigoPostal,
        string telefono,
        string? extension)
    {
        var claveNormalizada = NormalizarClave(clave);
        if (claveNormalizada.IsFailure)
        {
            return claveNormalizada.Error;
        }

        var nombreNormalizado = NormalizarTextoObligatorio(
            nombre, LongitudMaximaNombre, EntidadAcademicaErrors.NombreVacio, EntidadAcademicaErrors.NombreDemasiadoLargo);
        if (nombreNormalizado.IsFailure)
        {
            return nombreNormalizado.Error;
        }

        var calleNormalizada = NormalizarTextoObligatorio(
            calle, LongitudMaximaCalle, EntidadAcademicaErrors.CalleVacia, EntidadAcademicaErrors.CalleDemasiadoLarga);
        if (calleNormalizada.IsFailure)
        {
            return calleNormalizada.Error;
        }

        var numeroExteriorNormalizado = NormalizarNumeroExterior(numeroExterior);
        if (numeroExteriorNormalizado.IsFailure)
        {
            return numeroExteriorNormalizado.Error;
        }

        var coloniaNormalizada = NormalizarTextoObligatorio(
            colonia, LongitudMaximaColonia, EntidadAcademicaErrors.ColoniaVacia, EntidadAcademicaErrors.ColoniaDemasiadoLarga);
        if (coloniaNormalizada.IsFailure)
        {
            return coloniaNormalizada.Error;
        }

        var codigoPostalNormalizado = NormalizarCodigoPostal(codigoPostal);
        if (codigoPostalNormalizado.IsFailure)
        {
            return codigoPostalNormalizado.Error;
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

        return new DatosValidados(
            claveNormalizada.Value,
            nombreNormalizado.Value,
            calleNormalizada.Value,
            numeroExteriorNormalizado.Value,
            coloniaNormalizada.Value,
            codigoPostalNormalizado.Value,
            telefonoNormalizado.Value,
            extensionNormalizada.Value);
    }

    private static Result<string> NormalizarClave(string? clave)
    {
        var normalizada = Normalizacion.Recortar(clave).ToUpperInvariant();

        if (normalizada.Length == 0)
        {
            return EntidadAcademicaErrors.ClaveVacia;
        }

        if (normalizada.Length > LongitudMaximaClave)
        {
            return EntidadAcademicaErrors.ClaveDemasiadoLarga;
        }

        if (!normalizada.All(caracter => caracter is (>= 'A' and <= 'Z') or (>= '0' and <= '9')))
        {
            return EntidadAcademicaErrors.ClaveFormatoInvalido;
        }

        return normalizada;
    }

    private static Result<string> NormalizarTextoObligatorio(
        string? valor, int longitudMaxima, Error errorVacio, Error errorDemasiadoLargo)
    {
        var normalizado = Normalizacion.Texto(valor);

        if (normalizado.Length == 0)
        {
            return errorVacio;
        }

        if (normalizado.Length > longitudMaxima)
        {
            return errorDemasiadoLargo;
        }

        return normalizado;
    }

    private static Result<string?> NormalizarNumeroExterior(string? numeroExterior)
    {
        if (numeroExterior is null)
        {
            return Result.Success<string?>(null);
        }

        var normalizado = Normalizacion.Texto(numeroExterior);

        if (normalizado.Length == 0)
        {
            return EntidadAcademicaErrors.NumeroExteriorVacio;
        }

        if (normalizado.Length > LongitudMaximaNumeroExterior)
        {
            return EntidadAcademicaErrors.NumeroExteriorDemasiadoLargo;
        }

        return Result.Success<string?>(normalizado);
    }

    private static Result<string> NormalizarCodigoPostal(string? codigoPostal)
    {
        var recortado = Normalizacion.Recortar(codigoPostal);

        if (recortado.Length == 0)
        {
            return EntidadAcademicaErrors.CodigoPostalVacio;
        }

        if (recortado.Length != LongitudCodigoPostal || !Normalizacion.SonDigitos(recortado))
        {
            return EntidadAcademicaErrors.CodigoPostalFormatoInvalido;
        }

        return recortado;
    }

    private static Result<string> NormalizarTelefono(string? telefono)
    {
        var recortado = Normalizacion.Recortar(telefono);

        if (recortado.Length == 0)
        {
            return EntidadAcademicaErrors.TelefonoVacio;
        }

        if (recortado.Length != LongitudTelefono || !Normalizacion.SonDigitos(recortado))
        {
            return EntidadAcademicaErrors.TelefonoFormatoInvalido;
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
            return EntidadAcademicaErrors.ExtensionFormatoInvalido;
        }

        return Result.Success<string?>(recortada);
    }

    private sealed record DatosValidados(
        string Clave,
        string Nombre,
        string Calle,
        string? NumeroExterior,
        string Colonia,
        string CodigoPostal,
        string Telefono,
        string? Extension);
}
