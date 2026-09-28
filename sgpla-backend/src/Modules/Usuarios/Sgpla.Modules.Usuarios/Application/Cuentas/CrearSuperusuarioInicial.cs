using Microsoft.Extensions.Logging;
using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Usuarios.Application.Autenticacion;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Usuarios.Application.Cuentas;

internal sealed record CrearSuperusuarioInicialCommand(string Correo, string Nombre);

/// <summary>La temporal es <c>null</c> para DGAA y Entidad Académica (sección 7); aquí siempre trae valor.</summary>
internal sealed record CuentaCreada(int Id, string? ContrasenaTemporal);

/// <summary>Usado por el subcomando <c>bootstrap-superusuario</c> (Modulo_Usuarios.md, sección 8).</summary>
internal sealed class CrearSuperusuarioInicialHandler(
    IUsuarioRepository repositorio,
    IHasherContrasenas hasher,
    IGeneradorContrasenas generador,
    IUnitOfWork unidadDeTrabajo,
    ILogger<CrearSuperusuarioInicialHandler> logger) : ICommandHandler<CrearSuperusuarioInicialCommand, CuentaCreada>
{
    public async Task<Result<CuentaCreada>> HandleAsync(
        CrearSuperusuarioInicialCommand command, CancellationToken cancellationToken)
    {
        if (await repositorio.ContarSuperusuariosAsync(cancellationToken) > 0)
        {
            return UsuarioErrors.SuperusuarioExistente;
        }

        var temporal = generador.GenerarTemporal();
        var creado = Usuario.CrearSuperusuario(command.Correo, command.Nombre, hasher.Hashear(temporal));
        if (creado.IsFailure)
        {
            return creado.Error;
        }

        if (await repositorio.ExisteCorreoAsync(creado.Value.Correo, cancellationToken))
        {
            return UsuarioErrors.CorreoDuplicado;
        }

        repositorio.Agregar(creado.Value);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        logger.SuperusuarioInicialCreado(creado.Value.Id);

        return new CuentaCreada(creado.Value.Id, temporal);
    }
}

internal static partial class CrearSuperusuarioInicialLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Superusuario inicial creado: {UsuarioId}")]
    public static partial void SuperusuarioInicialCreado(this ILogger logger, int usuarioId);
}
