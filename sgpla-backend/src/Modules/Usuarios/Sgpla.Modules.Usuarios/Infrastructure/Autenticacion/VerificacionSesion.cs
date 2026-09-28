using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.Usuarios.Application.Cuentas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Usuarios.Infrastructure.Autenticacion;

/// <summary>Verificación contra la base en cada petición autenticada (Modulo_Usuarios.md, sección 6).</summary>
internal static class VerificacionSesion
{
    public const string ClaimCambioContrasenaPendiente = "cambio_contrasena_pendiente";
    public const string CodigoNoAutenticado = "Autenticacion.NoAutenticado";

    public static async Task OnTokenValidatedAsync(TokenValidatedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var principal = context.Principal;

        var sub = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var rolClaim = principal?.FindFirstValue("rol");
        if (sub is null
            || rolClaim is null
            || !int.TryParse(sub, CultureInfo.InvariantCulture, out var usuarioId)
            || !byte.TryParse(rolClaim, CultureInfo.InvariantCulture, out var rolNumero))
        {
            context.Fail("Token sin identidad válida.");
            return;
        }

        var repositorio = context.HttpContext.RequestServices.GetRequiredService<IUsuarioRepository>();
        var cuenta = await repositorio.ObtenerPorIdAsync(usuarioId, context.HttpContext.RequestAborted);

        if (cuenta is null)
        {
            context.Fail("La cuenta no existe.");
            return;
        }

        if ((byte)cuenta.Rol != rolNumero)
        {
            context.Fail("El rol no coincide con la cuenta.");
            return;
        }

        if (cuenta.Rol == Rol.Superusuario && cuenta.Credencial?.FechaEliminacion is not null)
        {
            context.Fail("La credencial está dada de baja.");
            return;
        }

        if (cuenta.Rol is Rol.Dgaa or Rol.EntidadAcademica)
        {
            var ambitos = context.HttpContext.RequestServices.GetRequiredService<IAmbitosInstitucionales>();
            var activo = cuenta.Rol == Rol.Dgaa
                ? await ambitos.AreaAcademicaActivaAsync(cuenta.AreaAcademicaId!.Value, context.HttpContext.RequestAborted)
                : await ambitos.EntidadAcademicaActivaAsync(cuenta.EntidadAcademicaId!.Value, context.HttpContext.RequestAborted);

            if (!activo)
            {
                context.Fail("El ámbito de la cuenta está dado de baja.");
                return;
            }
        }

        if (cuenta.CambioContrasenaPendiente && principal?.Identity is ClaimsIdentity identidad)
        {
            identidad.AddClaim(new Claim(ClaimCambioContrasenaPendiente, "true"));
        }
    }

    public static async Task OnChallengeAsync(JwtBearerChallengeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.HandleResponse();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;

        var problemDetailsService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "No autenticado",
                Detail = "Se requiere iniciar sesión.",
                Extensions = { [ErrorHttpExtensions.ExtensionCodigo] = CodigoNoAutenticado },
            },
        });
    }
}
