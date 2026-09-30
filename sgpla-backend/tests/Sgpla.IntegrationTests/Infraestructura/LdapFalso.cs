using Sgpla.Modules.Usuarios.Application.Autenticacion;

namespace Sgpla.IntegrationTests.Infraestructura;

/// <summary>
/// Reemplaza a <c>LdapAutenticador</c> en las pruebas: acepta
/// <see cref="ContrasenaValida"/>, responde <see cref="ResultadoLdap.NoDisponible"/> para un correo que empieza con
/// <c>ldap-caido</c> y <see cref="ResultadoLdap.CredencialesInvalidas"/> para todo lo demás.
/// </summary>
internal sealed class LdapFalso : ILdapAutenticador
{
    public const string ContrasenaValida = "Ld@pDePruebas2026!";

    public Task<ResultadoLdap> AutenticarAsync(string correo, string contrasena, CancellationToken cancellationToken)
    {
        if (correo.StartsWith("ldap-caido", StringComparison.Ordinal))
        {
            return Task.FromResult(ResultadoLdap.NoDisponible);
        }

        return Task.FromResult(
            string.Equals(contrasena, ContrasenaValida, StringComparison.Ordinal)
                ? ResultadoLdap.Autenticado
                : ResultadoLdap.CredencialesInvalidas);
    }
}
