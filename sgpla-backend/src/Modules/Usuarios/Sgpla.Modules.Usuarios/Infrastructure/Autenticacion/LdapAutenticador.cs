using System.DirectoryServices.Protocols;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sgpla.Modules.Usuarios.Application.Autenticacion;

namespace Sgpla.Modules.Usuarios.Infrastructure.Autenticacion;

/// <summary>Bind directo con el correo como identidad, igual que el sistema anterior de la UV (Modulo_Usuarios.md, decisión D2).</summary>
internal sealed class LdapAutenticador(IOptions<LdapOptions> opciones, ILogger<LdapAutenticador> logger) : ILdapAutenticador
{
    private const int CodigoCredencialesInvalidas = 49;
    private const int CodigoSinRespuesta = -1;

    public async Task<ResultadoLdap> AutenticarAsync(string correo, string contrasena, CancellationToken cancellationToken)
    {
        var actuales = opciones.Value;

        try
        {
            return await Task.Run(() => Autenticar(correo, contrasena, actuales), cancellationToken);
        }
        catch (LdapException excepcion) when (excepcion.ErrorCode == CodigoCredencialesInvalidas)
        {
            return ResultadoLdap.CredencialesInvalidas;
        }
        catch (LdapException excepcion)
        {
            logger.LdapNoDisponible(excepcion.ErrorCode);
            return ResultadoLdap.NoDisponible;
        }
        catch (TimeoutException)
        {
            logger.LdapNoDisponible(CodigoSinRespuesta);
            return ResultadoLdap.NoDisponible;
        }
    }

    private static ResultadoLdap Autenticar(string correo, string contrasena, LdapOptions opciones)
    {
        using var conexion = new LdapConnection(new LdapDirectoryIdentifier(opciones.Servidor, opciones.Puerto))
        {
            AuthType = AuthType.Basic,
            Timeout = TimeSpan.FromSeconds(opciones.TiempoEsperaSegundos),
        };
        conexion.SessionOptions.ProtocolVersion = 3;
        conexion.SessionOptions.ReferralChasing = ReferralChasingOptions.None;

        if (opciones.Seguridad == SeguridadLdap.Ldaps)
        {
            conexion.SessionOptions.SecureSocketLayer = true;
        }
        else if (opciones.Seguridad == SeguridadLdap.StartTls)
        {
            conexion.SessionOptions.StartTransportLayerSecurity(null);
        }

        conexion.Credential = new NetworkCredential(correo, contrasena);
        conexion.Bind();

        return ResultadoLdap.Autenticado;
    }
}

internal static partial class LdapAutenticadorLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "LDAP no disponible (código {CodigoLdap})")]
    public static partial void LdapNoDisponible(this ILogger logger, int codigoLdap);
}
