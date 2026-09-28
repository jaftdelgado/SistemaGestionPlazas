using Microsoft.Extensions.Logging;
using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Usuarios.Application.Autenticacion;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Usuarios.Application.Cuentas;

internal sealed record RestablecerContrasenaCommand(int Id);

internal sealed class RestablecerContrasenaHandler(
    IUsuarioRepository repositorio,
    IHasherContrasenas hasher,
    IGeneradorContrasenas generador,
    ICurrentUser usuarioActual,
    IUnitOfWork unidadDeTrabajo,
    ILogger<RestablecerContrasenaHandler> logger) : ICommandHandler<RestablecerContrasenaCommand, string>
{
    public async Task<Result<string>> HandleAsync(RestablecerContrasenaCommand command, CancellationToken cancellationToken)
    {
        if (command.Id == usuarioActual.Id)
        {
            return UsuarioErrors.RestablecimientoPropio;
        }

        var usuario = await repositorio.ObtenerPorIdAsync(command.Id, cancellationToken);
        if (usuario is null)
        {
            return UsuarioErrors.NoEncontrado(command.Id);
        }

        if (usuario.Rol != Rol.Superusuario)
        {
            return UsuarioErrors.RestablecimientoNoAplica;
        }

        var temporal = generador.GenerarTemporal();
        usuario.RestablecerContrasena(hasher.Hashear(temporal));
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        logger.ContrasenaRestablecida(usuario.Id, usuarioActual.Id);

        return temporal;
    }
}

internal static partial class RestablecerContrasenaLog
{
    [LoggerMessage(EventId = 1005, Level = LogLevel.Information, Message = "Contraseña restablecida del usuario {UsuarioId} por el usuario {PorUsuarioId}")]
    public static partial void ContrasenaRestablecida(this ILogger logger, int usuarioId, int porUsuarioId);
}
