using System.Diagnostics.CodeAnalysis;

namespace Sgpla.SharedKernel;

/// <summary>Categoría de un error de negocio. Decide el código HTTP de la respuesta.</summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
}

/// <summary>Error de negocio: un código estable, un mensaje en español para el usuario y su categoría.</summary>
[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Nombre fijado por ESTANDAR_MODULOS.md; solo se consume desde C#.")]
public record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
}

/// <summary>Error de validación de la entrada, con los mensajes agrupados por campo.</summary>
public sealed record ValidationError(IReadOnlyDictionary<string, string[]> Errores)
    : Error(CodigoEntradaInvalida, "La solicitud tiene datos inválidos.", ErrorType.Validation)
{
    public const string CodigoEntradaInvalida = "Validacion.EntradaInvalida";
}
