using FluentValidation;
using Microsoft.Extensions.Logging;
using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.Usuarios.Application.Autenticacion;
using Sgpla.Modules.Usuarios.Application.Cuentas;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Usuarios.Application.Sesion;

internal sealed record IniciarSesionCommand(string Correo, string Contrasena);

internal sealed record SesionIniciada(int UsuarioId, TokenEmitido Token);

internal sealed class IniciarSesionValidator : AbstractValidator<IniciarSesionCommand>
{
    public IniciarSesionValidator()
    {
        RuleFor(c => c.Correo).NotEmpty().WithName("correo");
        RuleFor(c => c.Contrasena).NotEmpty().WithName("contraseña");
    }
}

/// <summary>Sigue el orden de DATABASE.md §13.3: la contraseña nunca se guarda, se registra ni se devuelve.</summary>
internal sealed class IniciarSesionHandler(
    IUsuarioRepository repositorio,
    IAmbitosInstitucionales ambitos,
    ILdapAutenticador ldap,
    IHasherContrasenas hasher,
    IEmisorTokens emisorTokens,
    IUnitOfWork unidadDeTrabajo,
    ILogger<IniciarSesionHandler> logger) : ICommandHandler<IniciarSesionCommand, SesionIniciada>
{
    public async Task<Result<SesionIniciada>> HandleAsync(IniciarSesionCommand command, CancellationToken cancellationToken)
    {
        var correo = NormalizarCorreo(command.Correo);

        var usuario = await repositorio.ObtenerPorCorreoAsync(correo, cancellationToken);
        if (usuario is null)
        {
            logger.AccesoFallido(usuarioId: null, MotivoAccesoFallido.CuentaNoRegistrada);
            return UsuarioErrors.CuentaNoRegistrada;
        }

        VerificacionContrasena? verificacion = null;
        var fallido = usuario.Rol switch
        {
            Rol.Superusuario => ValidarSuperusuario(usuario, command.Contrasena, out verificacion),
            Rol.Dgaa or Rol.EntidadAcademica => await ValidarLdapAsync(usuario, command.Contrasena, cancellationToken),
            _ => throw new InvalidOperationException("Rol desconocido."),
        };

        if (fallido is not null)
        {
            logger.AccesoFallido(usuario.Id, fallido.Value);
            return fallido.Value switch
            {
                MotivoAccesoFallido.CuentaNoRegistrada => UsuarioErrors.CuentaNoRegistrada,
                MotivoAccesoFallido.AmbitoInactivo => UsuarioErrors.AmbitoInactivo,
                MotivoAccesoFallido.CredencialesInvalidas => UsuarioErrors.CredencialesInvalidas,
                MotivoAccesoFallido.LdapNoDisponible => UsuarioErrors.LdapNoDisponible,
                _ => throw new InvalidOperationException("Motivo de falla sin traducción."),
            };
        }

        if (verificacion == VerificacionContrasena.CorrectaRequiereRehash)
        {
            usuario.ActualizarVerificador(hasher.Hashear(command.Contrasena));
            await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
            logger.VerificadorActualizado(usuario.Id);
        }

        logger.AccesoExitoso(usuario.Id, usuario.Rol);
        return new SesionIniciada(usuario.Id, emisorTokens.Emitir(usuario));
    }

    private async Task<MotivoAccesoFallido?> ValidarLdapAsync(Usuario usuario, string contrasena, CancellationToken cancellationToken)
    {
        var ambitoActivo = usuario.Rol == Rol.Dgaa
            ? await ambitos.AreaAcademicaActivaAsync(usuario.AreaAcademicaId!.Value, cancellationToken)
            : await ambitos.EntidadAcademicaActivaAsync(usuario.EntidadAcademicaId!.Value, cancellationToken);

        if (!ambitoActivo)
        {
            return MotivoAccesoFallido.AmbitoInactivo;
        }

        var resultado = await ldap.AutenticarAsync(usuario.Correo, contrasena, cancellationToken);
        return resultado switch
        {
            ResultadoLdap.Autenticado => null,
            ResultadoLdap.CredencialesInvalidas => MotivoAccesoFallido.CredencialesInvalidas,
            ResultadoLdap.NoDisponible => MotivoAccesoFallido.LdapNoDisponible,
            _ => throw new InvalidOperationException("Resultado LDAP desconocido."),
        };
    }

    /// <summary>Verifica la contraseña una sola vez; el resultado sirve tanto para aceptar o rechazar como para decidir el rehash.</summary>
    private MotivoAccesoFallido? ValidarSuperusuario(Usuario usuario, string contrasena, out VerificacionContrasena? verificacion)
    {
        if (usuario.Credencial!.FechaEliminacion is not null)
        {
            verificacion = null;
            return MotivoAccesoFallido.CuentaNoRegistrada;
        }

        verificacion = hasher.Verificar(usuario.Credencial.Contrasena, contrasena);
        return verificacion == VerificacionContrasena.Incorrecta ? MotivoAccesoFallido.CredencialesInvalidas : null;
    }

    private static string NormalizarCorreo(string correo)
    {
        var recortado = Normalizacion.Recortar(correo).ToLowerInvariant();
        return recortado.Contains('@', StringComparison.Ordinal) ? recortado : $"{recortado}@uv.mx";
    }
}

internal enum MotivoAccesoFallido
{
    CuentaNoRegistrada,
    AmbitoInactivo,
    CredencialesInvalidas,
    LdapNoDisponible,
}

internal static partial class IniciarSesionLog
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Acceso exitoso del usuario {UsuarioId} con rol {Rol}")]
    public static partial void AccesoExitoso(this ILogger logger, int usuarioId, Rol rol);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Acceso fallido del usuario {UsuarioId} por {Motivo}")]
    public static partial void AccesoFallido(this ILogger logger, int? usuarioId, MotivoAccesoFallido motivo);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "Verificador actualizado (rehash) del usuario {UsuarioId}")]
    public static partial void VerificadorActualizado(this ILogger logger, int usuarioId);
}
