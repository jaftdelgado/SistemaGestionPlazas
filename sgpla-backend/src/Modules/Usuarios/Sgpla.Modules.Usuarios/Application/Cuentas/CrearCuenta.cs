using FluentValidation;
using Microsoft.Extensions.Logging;
using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.Usuarios.Application.Autenticacion;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Usuarios.Application.Cuentas;

internal sealed record CrearCuentaCommand(string Correo, string Nombre, byte RolId, int? AreaAcademicaId, int? EntidadAcademicaId);

internal sealed class CrearCuentaValidator : AbstractValidator<CrearCuentaCommand>
{
    public CrearCuentaValidator()
    {
        RuleFor(c => c.RolId).InclusiveBetween((byte)1, (byte)3).WithName("rol");

        RuleFor(c => c.AreaAcademicaId).NotNull().GreaterThan(0)
            .OverridePropertyName("areaAcademicaId").WithName("área académica")
            .When(c => c.RolId == (byte)Rol.Dgaa);
        RuleFor(c => c.AreaAcademicaId).Null()
            .WithMessage("El área académica solo aplica a cuentas DGAA.")
            .OverridePropertyName("areaAcademicaId").WithName("área académica")
            .When(c => c.RolId != (byte)Rol.Dgaa);

        RuleFor(c => c.EntidadAcademicaId).NotNull().GreaterThan(0)
            .OverridePropertyName("entidadAcademicaId").WithName("entidad académica")
            .When(c => c.RolId == (byte)Rol.EntidadAcademica);
        RuleFor(c => c.EntidadAcademicaId).Null()
            .WithMessage("La entidad académica solo aplica a cuentas de Entidad Académica.")
            .OverridePropertyName("entidadAcademicaId").WithName("entidad académica")
            .When(c => c.RolId != (byte)Rol.EntidadAcademica);
    }
}

internal sealed class CrearCuentaHandler(
    IUsuarioRepository repositorio,
    IAmbitosInstitucionales ambitos,
    IHasherContrasenas hasher,
    IGeneradorContrasenas generador,
    ICurrentUser usuarioActual,
    IUnitOfWork unidadDeTrabajo,
    ILogger<CrearCuentaHandler> logger) : ICommandHandler<CrearCuentaCommand, CuentaCreada>
{
    public async Task<Result<CuentaCreada>> HandleAsync(CrearCuentaCommand command, CancellationToken cancellationToken)
    {
        var rol = (Rol)command.RolId;
        var temporal = rol == Rol.Superusuario ? generador.GenerarTemporal() : null;

        var creado = rol switch
        {
            Rol.Superusuario => Usuario.CrearSuperusuario(command.Correo, command.Nombre, hasher.Hashear(temporal!)),
            Rol.Dgaa => Usuario.CrearDgaa(command.Correo, command.Nombre, command.AreaAcademicaId!.Value),
            Rol.EntidadAcademica => Usuario.CrearEntidadAcademica(command.Correo, command.Nombre, command.EntidadAcademicaId!.Value),
            _ => throw new InvalidOperationException("Rol desconocido."),
        };

        if (creado.IsFailure)
        {
            return creado.Error;
        }

        var usuario = creado.Value;

        if (usuario.Rol == Rol.Dgaa && !await ambitos.AreaAcademicaActivaAsync(usuario.AreaAcademicaId!.Value, cancellationToken))
        {
            return UsuarioErrors.AreaAcademicaInexistente;
        }

        if (usuario.Rol == Rol.EntidadAcademica
            && !await ambitos.EntidadAcademicaActivaAsync(usuario.EntidadAcademicaId!.Value, cancellationToken))
        {
            return UsuarioErrors.EntidadAcademicaInexistente;
        }

        if (await repositorio.ExisteCorreoAsync(usuario.Correo, cancellationToken))
        {
            return UsuarioErrors.CorreoDuplicado;
        }

        repositorio.Agregar(usuario);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        logger.CuentaCreada(usuario.Id, usuario.Rol, usuarioActual.Id);

        return new CuentaCreada(usuario.Id, temporal);
    }
}

internal static partial class CrearCuentaLog
{
    [LoggerMessage(EventId = 1006, Level = LogLevel.Information, Message = "Cuenta {UsuarioId} creada con rol {Rol} por el usuario {PorUsuarioId}")]
    public static partial void CuentaCreada(this ILogger logger, int usuarioId, Rol rol, int porUsuarioId);
}
