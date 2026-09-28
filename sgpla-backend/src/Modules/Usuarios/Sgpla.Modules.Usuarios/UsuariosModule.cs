using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Handlers;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.Usuarios.Application.Autenticacion;
using Sgpla.Modules.Usuarios.Application.Cuentas;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.Modules.Usuarios.Endpoints.Cuentas;
using Sgpla.Modules.Usuarios.Endpoints.Sesion;
using Sgpla.Modules.Usuarios.Infrastructure.Autenticacion;
using Sgpla.Modules.Usuarios.Infrastructure.Contratos;
using Sgpla.Modules.Usuarios.Infrastructure.Cuentas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Usuarios;

/// <summary>Punto de registro del módulo Usuarios en el host.</summary>
public static class UsuariosModule
{
    public const string Ruta = "/api/v1/usuarios";

    private const int LongitudMinimaClaveJwtEnBytes = 32;

    public static IServiceCollection AddUsuariosModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var ensamblado = typeof(UsuariosModule).Assembly;

        services.AddPersistenciaModulo(ensamblado);
        services.AddHandlersModulo(ensamblado);

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Seccion))
            .ValidateDataAnnotations()
            .Validate(
                o => Encoding.UTF8.GetByteCount(o.Clave) >= LongitudMinimaClaveJwtEnBytes,
                $"Jwt:Clave debe tener al menos {LongitudMinimaClaveJwtEnBytes} bytes en UTF-8.")
            .ValidateOnStart();

        services.AddOptions<Argon2Options>()
            .Bind(configuration.GetSection(Argon2Options.Seccion))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<LdapOptions>()
            .Bind(configuration.GetSection(LdapOptions.Seccion))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<LdapOptions>, LdapOptionsValidador>();

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IHasherContrasenas, HasherArgon2>();
        services.AddSingleton<IGeneradorContrasenas, GeneradorContrasenas>();
        services.AddScoped<ILdapAutenticador, LdapAutenticador>();
        services.AddScoped<IEmisorTokens, EmisorTokens>();

        services.AddScoped<IUsuariosDeAreaAcademica, UsuariosDeAmbito>();
        services.AddScoped<IUsuariosDeEntidadAcademica, UsuariosDeAmbito>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, UsuarioActual>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((jwtBearerOptions, jwtOptions) =>
            {
                var opciones = jwtOptions.Value;
                jwtBearerOptions.MapInboundClaims = false;
                jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = opciones.Emisor,
                    ValidAudience = opciones.Audiencia,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opciones.Clave)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
                jwtBearerOptions.Events = new JwtBearerEvents
                {
                    OnTokenValidated = VerificacionSesion.OnTokenValidatedAsync,
                    OnChallenge = VerificacionSesion.OnChallengeAsync,
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Politicas.SesionIniciada, politica => politica.RequireAuthenticatedUser())
            .AddPolicy(Politicas.Autenticado, politica => politica
                .RequireAuthenticatedUser()
                .AddRequirements(new SinCambioPendienteRequirement()))
            .AddPolicy(Politicas.Superusuario, politica => politica
                .RequireAuthenticatedUser()
                .AddRequirements(new SinCambioPendienteRequirement())
                .RequireClaim("rol", ((byte)Rol.Superusuario).ToString(CultureInfo.InvariantCulture)));

        services.AddSingleton<IAuthorizationHandler, SinCambioPendienteHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, AutorizacionResultHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapUsuariosEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup(Ruta);
        grupo.MapSesionEndpoints();
        grupo.MapCuentaEndpoints();

        return endpoints;
    }

    /// <summary>Subcomando <c>bootstrap-superusuario</c> de la imagen de la API (Modulo_Usuarios.md, sección 8).</summary>
    public static async Task<int> EjecutarBootstrapAsync(IServiceProvider servicios, TextWriter salida, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(salida);

        var configuracion = servicios.GetRequiredService<IConfiguration>();
        var correo = configuracion["SGPLA_BOOTSTRAP_CORREO"];
        var nombre = configuracion["SGPLA_BOOTSTRAP_NOMBRE"];
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(nombre))
        {
            await salida.WriteLineAsync("Faltan SGPLA_BOOTSTRAP_CORREO o SGPLA_BOOTSTRAP_NOMBRE.");
            return 2;
        }

        await using var alcance = servicios.CreateAsyncScope();
        var handler = alcance.ServiceProvider
            .GetRequiredService<ICommandHandler<CrearSuperusuarioInicialCommand, CuentaCreada>>();
        var resultado = await handler.HandleAsync(new CrearSuperusuarioInicialCommand(correo, nombre), cancellationToken);

        if (resultado.IsFailure)
        {
            await salida.WriteLineAsync(resultado.Error.Message);
            return resultado.Error.Code == UsuarioErrors.SuperusuarioExistente.Code ? 0 : 1;
        }

        await salida.WriteLineAsync($"Superusuario {resultado.Value.Id} creado.");
        await salida.WriteLineAsync($"Contraseña temporal: {resultado.Value.ContrasenaTemporal} (cámbiala al iniciar sesión)");
        return 0;
    }
}
