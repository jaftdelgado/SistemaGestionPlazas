using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Sgpla.Modules.Usuarios.Application.Autenticacion;
using Sgpla.Modules.Usuarios.Domain.Cuentas;

namespace Sgpla.Modules.Usuarios.Infrastructure.Autenticacion;

/// <summary>El token no lleva el estado de la contraseña: la base es la fuente.</summary>
internal sealed class EmisorTokens(IOptions<JwtOptions> opciones, TimeProvider reloj) : IEmisorTokens
{
    public TokenEmitido Emitir(Usuario usuario)
    {
        var actuales = opciones.Value;
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var expiracion = ahora.AddHours(actuales.DuracionHoras);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString(CultureInfo.InvariantCulture)),
            new("rol", ((byte)usuario.Rol).ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (usuario.AreaAcademicaId is { } areaAcademicaId)
        {
            claims.Add(new Claim("area_academica_id", areaAcademicaId.ToString(CultureInfo.InvariantCulture)));
        }

        if (usuario.EntidadAcademicaId is { } entidadAcademicaId)
        {
            claims.Add(new Claim("entidad_academica_id", entidadAcademicaId.ToString(CultureInfo.InvariantCulture)));
        }

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(actuales.Clave)), SecurityAlgorithms.HmacSha256);

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = actuales.Emisor,
            Audience = actuales.Audiencia,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = ahora,
            Expires = expiracion,
            SigningCredentials = credenciales,
        });

        return new TokenEmitido(token, expiracion);
    }
}
