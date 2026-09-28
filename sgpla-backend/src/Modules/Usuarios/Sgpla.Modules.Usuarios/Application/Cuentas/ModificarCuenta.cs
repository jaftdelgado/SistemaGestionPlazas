using Microsoft.Extensions.Logging;
using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Usuarios.Application.Cuentas;

internal sealed record ModificarCuentaCommand(int Id, string Nombre);

internal sealed class ModificarCuentaHandler(
    IUsuarioRepository repositorio,
    ICurrentUser usuarioActual,
    IUnitOfWork unidadDeTrabajo,
    ILogger<ModificarCuentaHandler> logger) : ICommandHandler<ModificarCuentaCommand>
{
    public async Task<Result> HandleAsync(ModificarCuentaCommand command, CancellationToken cancellationToken)
    {
        var usuario = await repositorio.ObtenerPorIdAsync(command.Id, cancellationToken);
        if (usuario is null)
        {
            return UsuarioErrors.NoEncontrado(command.Id);
        }

        var modificado = usuario.CambiarNombre(command.Nombre);
        if (modificado.IsFailure)
        {
            return modificado;
        }

        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        logger.CuentaModificada(usuario.Id, usuarioActual.Id);

        return Result.Success();
    }
}

internal static partial class ModificarCuentaLog
{
    [LoggerMessage(EventId = 1007, Level = LogLevel.Information, Message = "Cuenta {UsuarioId} modificada por el usuario {PorUsuarioId}")]
    public static partial void CuentaModificada(this ILogger logger, int usuarioId, int porUsuarioId);
}
