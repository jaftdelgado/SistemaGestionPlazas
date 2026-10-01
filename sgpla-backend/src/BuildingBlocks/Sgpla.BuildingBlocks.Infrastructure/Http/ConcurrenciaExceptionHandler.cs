using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Sgpla.BuildingBlocks.Infrastructure.Http;

/// <summary>
/// Traduce a 409 un choque de concurrencia optimista (<see cref="DbUpdateConcurrencyException"/>): la fila cambió entre
/// que se leyó y que se guardó. No registra nada, porque el cliente ya recibe el 409.
/// </summary>
public sealed class ConcurrenciaExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public const string Codigo = "Persistencia.ModificacionConcurrente";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (exception is not DbUpdateConcurrencyException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflicto",
                Detail = "El registro cambió mientras lo modificabas. Consulta de nuevo y repite la operación.",
                Extensions = { [ErrorHttpExtensions.ExtensionCodigo] = Codigo },
            },
        });
    }
}
