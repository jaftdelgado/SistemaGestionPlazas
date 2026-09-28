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
    /// <summary>Nunca se persiste: solo permite validar con la fábrica antes de calcular el hash real.</summary>
    private const string VerificadorProvisional = "sin-asignar";

    public async Task<Result<CuentaCreada>> HandleAsync(
        CrearSuperusuarioInicialCommand command, CancellationToken cancellationToken)
    {
        if (await repositorio.ContarSuperusuariosAsync(cancellationToken) > 0)
        {
            return UsuarioErrors.SuperusuarioExistente;
        }

        var creado = Usuario.CrearSuperusuario(command.Correo, command.Nombre, VerificadorProvisional);
        if (creado.IsFailure)
        {
            return creado.Error;
        }

        var usuario = creado.Value;

        if (await repositorio.ExisteCorreoAsync(usuario.Correo, cancellationToken))
        {
            return UsuarioErrors.CorreoDuplicado;
        }

        // El verificador real se calcula solo cuando ya se sabe que la cuenta se va a crear.
        var temporal = generador.GenerarTemporal();
        usuario.RestablecerContrasena(hasher.Hashear(temporal));

        repositorio.Agregar(usuario);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        logger.SuperusuarioInicialCreado(usuario.Id);

        return new CuentaCreada(usuario.Id, temporal);
    }
}

internal static partial class CrearSuperusuarioInicialLog
{
    [LoggerMessage(EventId = 1009, Level = LogLevel.Information, Message = "Superusuario inicial creado: {UsuarioId}")]
    public static partial void SuperusuarioInicialCreado(this ILogger logger, int usuarioId);
}
