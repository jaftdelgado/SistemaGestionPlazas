using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Sgpla.BuildingBlocks.Infrastructure.Http;

/// <summary>
/// Traduce a 409 una violación de unicidad de SQL Server (2601/2627). Es la última defensa cuando dos peticiones
/// simultáneas pasan la comprobación previa del handler. Solo registra el nombre de la restricción, porque el
/// mensaje de SQL Server incluye el valor duplicado.
/// </summary>
public sealed partial class ViolacionUnicidadExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<ViolacionUnicidadExceptionHandler> logger) : IExceptionHandler
{
    public const string Codigo = "Persistencia.ValorDuplicado";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (exception is not DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } sql })
        {
            return false;
        }

        var restriccion = NombreRestriccion().Match(sql.Message);
        logger.ViolacionUnicidad(restriccion.Success ? restriccion.Groups["nombre"].Value : "desconocida");

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflicto",
                Detail = "Ya existe un registro con esos datos.",
                Extensions = { [ErrorHttpExtensions.ExtensionCodigo] = Codigo },
            },
        });
    }

    // 2627: "Violation of UNIQUE KEY constraint 'uq_...'"; 2601: "... with unique index 'ux_...'".
    [GeneratedRegex("(?:constraint|index) '(?<nombre>[^']+)'")]
    private static partial Regex NombreRestriccion();
}

internal static partial class ViolacionUnicidadLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Violación de unicidad en la restricción {Restriccion}; se respondió 409")]
    public static partial void ViolacionUnicidad(this ILogger logger, string restriccion);
}
