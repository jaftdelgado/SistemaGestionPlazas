using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Sgpla.SharedKernel;

namespace Sgpla.BuildingBlocks.Infrastructure.Http;

public static class ErrorHttpExtensions
{
    /// <summary>Nombre de la extensión de ProblemDetails que lleva el <see cref="Error.Code"/>.</summary>
    public const string ExtensionCodigo = "codigo";

    /// <summary>
    /// Traduce un error de negocio a ProblemDetails: 400, 404 o 409 según su tipo, con la extensión
    /// <c>codigo</c> y, en los de validación, los errores por campo (los de los validators o el
    /// <see cref="Error.Campo"/> de un error de dominio). El <c>traceId</c> lo agrega el host.
    /// </summary>
    public static ProblemHttpResult ToProblem(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var (estado, titulo) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Solicitud inválida"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "No encontrado"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflicto"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "No autenticado"),
            ErrorType.Unavailable => (StatusCodes.Status503ServiceUnavailable, "Servicio no disponible"),
            _ => throw new ArgumentOutOfRangeException(nameof(error), error.Type, "Tipo de error sin código HTTP."),
        };

        IReadOnlyDictionary<string, string[]>? errores = error switch
        {
            ValidationError validacion => validacion.Errores,
            { Campo: { } campo } => new Dictionary<string, string[]>(StringComparer.Ordinal) { [campo] = [error.Message] },
            _ => null,
        };

        var problema = errores is null
            ? new ProblemDetails()
            : new HttpValidationProblemDetails(errores.ToDictionary(
                campo => NombreCampo(campo.Key),
                campo => campo.Value,
                StringComparer.Ordinal));

        problema.Status = estado;
        problema.Title = titulo;
        problema.Detail = error.Message;
        problema.Extensions[ExtensionCodigo] = error.Code;

        return TypedResults.Problem(problema);
    }

    /// <summary>Los campos se reportan como en el JSON: <c>Nombre</c> pasa a <c>nombre</c>.</summary>
    private static string NombreCampo(string propiedad) =>
        string.Join('.', propiedad.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
