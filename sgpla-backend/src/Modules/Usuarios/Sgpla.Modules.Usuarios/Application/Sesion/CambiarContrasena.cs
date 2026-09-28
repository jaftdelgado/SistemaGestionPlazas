using Microsoft.Extensions.Logging;
using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Usuarios.Application.Autenticacion;
using Sgpla.Modules.Usuarios.Application.Cuentas;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Usuarios.Application.Sesion;

internal sealed record CambiarContrasenaCommand(string ContrasenaActual, string ContrasenaNueva);

/// <summary>Solo para el Superusuario en sesión: DGAA y Entidad Académica cambian su contraseña en la UV.</summary>
internal sealed class CambiarContrasenaHandler(
    ICurrentUser usuarioActual,
    IUsuarioRepository repositorio,
    IHasherContrasenas hasher,
    IEmisorTokens emisorTokens,
    IUnitOfWork unidadDeTrabajo,
    TimeProvider reloj,
    ILogger<CambiarContrasenaHandler> logger) : ICommandHandler<CambiarContrasenaCommand, SesionIniciada>
{
    public async Task<Result<SesionIniciada>> HandleAsync(CambiarContrasenaCommand command, CancellationToken cancellationToken)
    {
        var usuario = await repositorio.ObtenerPorIdAsync(usuarioActual.Id, cancellationToken);
        if (usuario is null)
        {
            return UsuarioErrors.CuentaNoRegistrada;
        }

        if (usuario.Rol != Rol.Superusuario)
        {
            return UsuarioErrors.CambioContrasenaNoAplica;
        }

        if (hasher.Verificar(usuario.Credencial!.Contrasena, command.ContrasenaActual) == VerificacionContrasena.Incorrecta)
        {
            return UsuarioErrors.ContrasenaActualIncorrecta;
        }

        var politica = PoliticaContrasena.Validar(command.ContrasenaNueva);
        if (politica.IsFailure)
        {
            return politica.Error;
        }

        if (string.Equals(command.ContrasenaNueva, command.ContrasenaActual, StringComparison.Ordinal))
        {
            return UsuarioErrors.ContrasenaNuevaIgualActual;
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var instante = ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond));

        usuario.EstablecerContrasena(hasher.Hashear(command.ContrasenaNueva), instante);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        logger.ContrasenaCambiada(usuario.Id);

        return new SesionIniciada(usuario.Id, emisorTokens.Emitir(usuario));
    }
}

internal static partial class CambiarContrasenaLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Contraseña cambiada del usuario {UsuarioId}")]
    public static partial void ContrasenaCambiada(this ILogger logger, int usuarioId);
}
