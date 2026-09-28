using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Usuarios.Application.Autenticacion;
using Sgpla.Modules.Usuarios.Application.Cuentas;
using Sgpla.Modules.Usuarios.Domain.Cuentas;

namespace Sgpla.IntegrationTests.Infraestructura;

/// <summary>Host de la API en memoria apuntando a la base de datos del contenedor.</summary>
public sealed class SgplaApiFactory(SqlServerFixture sqlServer) : WebApplicationFactory<Program>
{
    /// <summary>Contraseña conocida de <see cref="CrearSuperusuarioAsync"/>, para las pruebas de inicio de sesión.</summary>
    public const string ContrasenaConocidaSuperusuario = "Temp0ral!Conocida";

    /// <summary>
    /// Ruta protegida solo con <see cref="Politicas.Autenticado"/>, registrada nada más para las pruebas (Modulo_Usuarios.md,
    /// sección 13, PR 1): mientras el PR 1 no protege ningún endpoint real, sirve para probar el 403 de contraseña
    /// pendiente. El PR 2 la reemplaza por <c>GET /cuentas</c>.
    /// </summary>
    public const string RutaDePruebaAutenticada = "/pruebas/autenticado";

    /// <summary>Logs emitidos por el host, con sus scopes.</summary>
    public ProveedorLogsEnMemoria Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Sgpla", sqlServer.CadenaConexion);

        // Jwt:Clave y Ldap:Servidor son obligatorios y nunca van en appsettings*.json (Modulo_Usuarios.md, sección 4).
        builder.UseSetting("Jwt:Clave", "clave-de-pruebas-de-integracion-nunca-usar-en-produccion");
        builder.UseSetting("Ldap:Servidor", "ldap-de-pruebas.invalido");

        // Para BootstrapTests: la base compartida siempre tiene Superusuarios, así que el bootstrap nunca los crea.
        builder.UseSetting("SGPLA_BOOTSTRAP_CORREO", "bootstrap@sgpla-pruebas.mx");
        builder.UseSetting("SGPLA_BOOTSTRAP_NOMBRE", "Bootstrap de pruebas");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ILdapAutenticador>();
            services.AddScoped<ILdapAutenticador, LdapFalso>();
            services.AddSingleton<IStartupFilter, RutaDePruebaStartupFilter>();
        });

        // La regla es solo para este proveedor: captura desde Information sin cambiar lo que muestra la consola.
        builder.ConfigureLogging(logging => logging
            .AddProvider(Logs)
            .AddFilter<ProveedorLogsEnMemoria>(category: null, LogLevel.Information));
    }

    /// <summary>Crea un Superusuario con la contraseña ya cambiada y un cliente con su token.</summary>
    public async Task<HttpClient> CrearClienteSuperusuarioAsync()
    {
        await using var alcance = Services.CreateAsyncScope();
        var repositorio = alcance.ServiceProvider.GetRequiredService<IUsuarioRepository>();
        var hasher = alcance.ServiceProvider.GetRequiredService<IHasherContrasenas>();
        var unidadDeTrabajo = alcance.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var reloj = alcance.ServiceProvider.GetRequiredService<TimeProvider>();

        var correo = DatosUnicos.Correo("sgpla-pruebas.mx");
        var usuario = Usuario.CrearSuperusuario(correo, "Superusuario de prueba", hasher.Hashear(Guid.NewGuid().ToString())).Value;
        usuario.EstablecerContrasena(hasher.Hashear(ContrasenaConocidaSuperusuario), reloj.GetUtcNow().UtcDateTime);
        repositorio.Agregar(usuario);
        await unidadDeTrabajo.SaveChangesAsync(CancellationToken.None);

        return ClienteConToken(alcance.ServiceProvider.GetRequiredService<IEmisorTokens>().Emitir(usuario));
    }

    /// <summary>Crea una cuenta DGAA del área indicada y un cliente con su token.</summary>
    public async Task<HttpClient> CrearClienteDgaaAsync(int areaAcademicaId)
    {
        await using var alcance = Services.CreateAsyncScope();
        var repositorio = alcance.ServiceProvider.GetRequiredService<IUsuarioRepository>();
        var unidadDeTrabajo = alcance.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var usuario = Usuario.CrearDgaa(DatosUnicos.Correo("uv.mx"), "DGAA de prueba", areaAcademicaId).Value;
        repositorio.Agregar(usuario);
        await unidadDeTrabajo.SaveChangesAsync(CancellationToken.None);

        return ClienteConToken(alcance.ServiceProvider.GetRequiredService<IEmisorTokens>().Emitir(usuario));
    }

    /// <summary>Crea una cuenta de Entidad Académica de la entidad indicada y un cliente con su token.</summary>
    public async Task<HttpClient> CrearClienteEntidadAcademicaAsync(int entidadAcademicaId)
    {
        await using var alcance = Services.CreateAsyncScope();
        var repositorio = alcance.ServiceProvider.GetRequiredService<IUsuarioRepository>();
        var unidadDeTrabajo = alcance.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var usuario = Usuario.CrearEntidadAcademica(DatosUnicos.Correo("uv.mx"), "Entidad de prueba", entidadAcademicaId).Value;
        repositorio.Agregar(usuario);
        await unidadDeTrabajo.SaveChangesAsync(CancellationToken.None);

        return ClienteConToken(alcance.ServiceProvider.GetRequiredService<IEmisorTokens>().Emitir(usuario));
    }

    /// <summary>Crea un Superusuario con contraseña temporal conocida, para las pruebas de inicio de sesión.</summary>
    public async Task<string> CrearSuperusuarioAsync()
    {
        await using var alcance = Services.CreateAsyncScope();
        var repositorio = alcance.ServiceProvider.GetRequiredService<IUsuarioRepository>();
        var hasher = alcance.ServiceProvider.GetRequiredService<IHasherContrasenas>();
        var unidadDeTrabajo = alcance.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var correo = DatosUnicos.Correo("sgpla-pruebas.mx");
        var usuario = Usuario.CrearSuperusuario(
            correo, "Superusuario de prueba", hasher.Hashear(ContrasenaConocidaSuperusuario)).Value;
        repositorio.Agregar(usuario);
        await unidadDeTrabajo.SaveChangesAsync(CancellationToken.None);

        return correo;
    }

    private HttpClient ClienteConToken(TokenEmitido token)
    {
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return cliente;
    }

    private sealed class RutaDePruebaStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.UseEndpoints(endpoints => endpoints
                .MapGet(RutaDePruebaAutenticada, () => Results.NoContent())
                .RequireAuthorization(Politicas.Autenticado));
        };
    }
}
