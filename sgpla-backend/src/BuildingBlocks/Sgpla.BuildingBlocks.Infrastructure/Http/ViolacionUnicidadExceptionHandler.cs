using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Sgpla.BuildingBlocks.Infrastructure.Http;

/// <summary>
/// Traduce a 409 una violación de unicidad de PostgreSQL (SQLSTATE 23505). Es la última defensa cuando dos peticiones
/// simultáneas pasan la comprobación previa del handler. Solo registra el nombre de la restricción: el detalle y el
/// mensaje de PostgreSQL incluyen el valor duplicado, y la cadena de conexión no debe activar Include Error Detail.
/// </summary>
public sealed class ViolacionUnicidadExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<ViolacionUnicidadExceptionHandler> logger) : IExceptionHandler
{
    public const string Codigo = "Persistencia.ValorDuplicado";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (exception is not DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg })
        {
            return false;
        }

        logger.ViolacionUnicidad(pg.ConstraintName ?? "desconocida");

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
}

internal static partial class ViolacionUnicidadLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Violación de unicidad en la restricción {Restriccion}; se respondió 409")]
    public static partial void ViolacionUnicidad(this ILogger logger, string restriccion);
}
