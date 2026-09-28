using Microsoft.Extensions.Logging;
using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Usuarios.Application.Cuentas;

internal sealed record DarDeBajaCuentaCommand(int Id);

internal sealed class DarDeBajaCuentaHandler(
    IUsuarioRepository repositorio,
    ICurrentUser usuarioActual,
    IUnitOfWork unidadDeTrabajo,
    TimeProvider reloj,
    ILogger<DarDeBajaCuentaHandler> logger) : ICommandHandler<DarDeBajaCuentaCommand>
{
    public async Task<Result> HandleAsync(DarDeBajaCuentaCommand command, CancellationToken cancellationToken)
    {
        if (command.Id == usuarioActual.Id)
        {
            return UsuarioErrors.BajaPropia;
        }

        var usuario = await repositorio.ObtenerPorIdAsync(command.Id, cancellationToken);
        if (usuario is null)
        {
            return UsuarioErrors.NoEncontrado(command.Id);
        }

        if (usuario.Rol == Rol.Superusuario && await repositorio.ContarSuperusuariosAsync(cancellationToken) <= 1)
        {
            return UsuarioErrors.UltimoSuperusuario;
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var instante = ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond));

        usuario.DarDeBaja(instante);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        logger.CuentaDadaDeBaja(usuario.Id, usuarioActual.Id);

        return Result.Success();
    }
}

internal static partial class DarDeBajaCuentaLog
{
    [LoggerMessage(EventId = 1008, Level = LogLevel.Information, Message = "Cuenta {UsuarioId} dada de baja por el usuario {PorUsuarioId}")]
    public static partial void CuentaDadaDeBaja(this ILogger logger, int usuarioId, int porUsuarioId);
}
