using System.Diagnostics.CodeAnalysis;
using System.DirectoryServices.Protocols;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sgpla.Modules.Usuarios.Application.Autenticacion;

namespace Sgpla.Modules.Usuarios.Infrastructure.Autenticacion;

/// <summary>Bind directo con el correo como identidad, igual que el sistema anterior de la UV.</summary>
internal sealed class LdapAutenticador(IOptions<LdapOptions> opciones, ILogger<LdapAutenticador> logger) : ILdapAutenticador
{
    private const int CodigoCredencialesInvalidas = 49;
    private const int CodigoSinRespuesta = -1;

    /// <summary>
    /// Costura para las pruebas: sustituye la conexión y el bind reales sin exponer una interfaz pública ni cambiar
    /// <see cref="ILdapAutenticador"/>.
    /// </summary>
    internal Action<string, string, LdapOptions> ConexionYBind { get; set; } = Autenticar;

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Cualquier falla técnica al conectar (DLL nativa faltante, error de inicialización) se traduce a " +
            "NoDisponible en vez de un 500; OperationCanceledException queda excluida y se propaga.")]
    public async Task<ResultadoLdap> AutenticarAsync(string correo, string contrasena, CancellationToken cancellationToken)
    {
        var actuales = opciones.Value;

        try
        {
            await Task.Run(() => ConexionYBind(correo, contrasena, actuales), cancellationToken);
            return ResultadoLdap.Autenticado;
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
        catch (DirectoryException)
        {
            // DirectoryOperationException (por ejemplo, de StartTransportLayerSecurity) y cualquier otra DirectoryException
            // sin un código LDAP propio.
            logger.LdapNoDisponible(CodigoSinRespuesta);
            return ResultadoLdap.NoDisponible;
        }
        catch (TimeoutException)
        {
            logger.LdapNoDisponible(CodigoSinRespuesta);
            return ResultadoLdap.NoDisponible;
        }
        catch (Exception excepcion) when (excepcion is not OperationCanceledException)
        {
            // Cubre DllNotFoundException y TypeInitializationException si falta libldap en la imagen, y cualquier otra
            // falla técnica de la biblioteca nativa.
            logger.LdapNoDisponible(CodigoSinRespuesta);
            return ResultadoLdap.NoDisponible;
        }
    }

    private static void Autenticar(string correo, string contrasena, LdapOptions opciones)
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
    }
}

internal static partial class LdapAutenticadorLog
{
    [LoggerMessage(EventId = 1010, Level = LogLevel.Warning, Message = "LDAP no disponible (código {CodigoLdap})")]
    public static partial void LdapNoDisponible(this ILogger logger, int codigoLdap);
}
