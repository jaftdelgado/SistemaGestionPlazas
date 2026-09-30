using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Sgpla.BuildingBlocks.Infrastructure.Http;

namespace Sgpla.Modules.Usuarios.Infrastructure.Autenticacion;

/// <summary>Distingue la falta de cambio de contraseña de cualquier otra falla de autorización.</summary>
internal sealed class SinCambioPendienteRequirement : IAuthorizationRequirement;

internal sealed class SinCambioPendienteHandler : AuthorizationHandler<SinCambioPendienteRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, SinCambioPendienteRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.User.HasClaim(claim => claim.Type == VerificacionSesion.ClaimCambioContrasenaPendiente))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Responde el 403 con <c>ProblemDetails</c>: distingue <see cref="SinCambioPendienteRequirement"/> de cualquier
/// otra falla, y delega el resto al manejador por omisión.
/// </summary>
internal sealed class AutorizacionResultHandler : IAuthorizationMiddlewareResultHandler
{
    public const string CodigoCambioContrasenaPendiente = "Autenticacion.CambioContrasenaPendiente";
    public const string CodigoSinPermiso = "Autorizacion.SinPermiso";

    private readonly AuthorizationMiddlewareResultHandler _porOmision = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(authorizeResult);

        if (!authorizeResult.Forbidden)
        {
            await _porOmision.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        var fallaCambioPendiente = authorizeResult.AuthorizationFailure?.FailedRequirements
            .OfType<SinCambioPendienteRequirement>()
            .Any() ?? false;

        var (codigo, detalle) = fallaCambioPendiente
            ? (CodigoCambioContrasenaPendiente, "Debes cambiar tu contraseña temporal antes de continuar.")
            : (CodigoSinPermiso, "No tienes permiso para realizar esta operación.");

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        var problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Prohibido",
                Detail = detalle,
                Extensions = { [ErrorHttpExtensions.ExtensionCodigo] = codigo },
            },
        });
    }
}
