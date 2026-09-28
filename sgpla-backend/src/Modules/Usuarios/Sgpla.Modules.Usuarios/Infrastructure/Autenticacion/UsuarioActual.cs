using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Sgpla.BuildingBlocks.Application;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Usuarios.Infrastructure.Autenticacion;

internal sealed class UsuarioActual(IHttpContextAccessor contextoHttp) : ICurrentUser
{
    public int Id => ObtenerClaim(JwtRegisteredClaimNames.Sub);

    public Rol Rol => (Rol)(byte)ObtenerClaim("rol");

    public int? AreaAcademicaId => ObtenerClaimOpcional("area_academica_id");

    public int? EntidadAcademicaId => ObtenerClaimOpcional("entidad_academica_id");

    private ClaimsPrincipal Sesion =>
        contextoHttp.HttpContext?.User.Identity?.IsAuthenticated == true
            ? contextoHttp.HttpContext.User
            : throw new InvalidOperationException("No hay una sesión autenticada en la petición actual.");

    private int ObtenerClaim(string tipo)
    {
        var valor = Sesion.FindFirstValue(tipo)
            ?? throw new InvalidOperationException($"La sesión no tiene el claim '{tipo}'.");
        return int.Parse(valor, CultureInfo.InvariantCulture);
    }

    private int? ObtenerClaimOpcional(string tipo)
    {
        var valor = Sesion.FindFirstValue(tipo);
        return valor is null ? null : int.Parse(valor, CultureInfo.InvariantCulture);
    }
}
