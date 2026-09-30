using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Usuarios.Application.Autenticacion;
using Sgpla.Modules.Usuarios.Application.Cuentas;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.SharedKernel;

namespace Sgpla.IntegrationTests.Infraestructura;

/// <summary>Host de la API en memoria apuntando a la base de datos del contenedor.</summary>
public sealed class SgplaApiFactory(SqlServerFixture sqlServer) : WebApplicationFactory<Program>
{
    private readonly string _rutaAlmacenamiento = Path.Combine(
        Path.GetTempPath(), "sgpla-archivos", Guid.NewGuid().ToString("N"));

    /// <summary>Contraseña conocida de <see cref="CrearSuperusuarioAsync"/>, para las pruebas de inicio de sesión.</summary>
    public const string ContrasenaConocidaSuperusuario = "Temp0ral!Conocida";

    /// <summary>Misma clave que <see cref="ConfigureWebHost"/> fija en <c>Jwt:Clave</c>, para firmar tokens de prueba a mano.</summary>
    public const string ClaveJwtDePrueba = "clave-de-pruebas-de-integracion-nunca-usar-en-produccion";

    /// <summary>Correo fijo de <c>SGPLA_BOOTSTRAP_CORREO</c> en las pruebas, para comprobar que el bootstrap no lo usa.</summary>
    public const string CorreoDeBootstrap = "bootstrap@sgpla-pruebas.mx";

    /// <summary>Logs emitidos por el host, con sus scopes.</summary>
    public ProveedorLogsEnMemoria Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Sgpla", sqlServer.CadenaConexion);

        // Jwt:Clave y Ldap:Servidor son obligatorios y nunca van en appsettings*.json.
        builder.UseSetting("Jwt:Clave", ClaveJwtDePrueba);
        builder.UseSetting("Ldap:Servidor", "ldap-de-pruebas.invalido");
        builder.UseSetting("SolicitudesApertura:PeriodoActual", SqlServerFixture.ClavePeriodoActual);
        builder.UseSetting("SolicitudesApertura:PeriodoSiguiente", SqlServerFixture.ClavePeriodoSiguiente);
        builder.UseSetting("Almacenamiento:RutaBase", _rutaAlmacenamiento);

        // Para BootstrapTests: la base compartida siempre tiene Superusuarios, así que el bootstrap nunca los crea.
        builder.UseSetting("SGPLA_BOOTSTRAP_CORREO", CorreoDeBootstrap);
        builder.UseSetting("SGPLA_BOOTSTRAP_NOMBRE", "Bootstrap de pruebas");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ILdapAutenticador>();
            services.AddScoped<ILdapAutenticador, LdapFalso>();
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

    /// <summary>
    /// Firma un token ya vencido con la misma clave y el mismo emisor que usan las pruebas, para provocar el 401 por
    /// expiración sin tocar el reloj de la aplicación.
    /// </summary>
    public static string EmitirTokenVencido(int usuarioId, Rol rol)
    {
        var vencimiento = DateTime.UtcNow.AddMinutes(-10);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuarioId.ToString(CultureInfo.InvariantCulture)),
            new("rol", ((byte)rol).ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ClaveJwtDePrueba)), SecurityAlgorithms.HmacSha256);

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "sgpla",
            Audience = "sgpla-web",
            Subject = new ClaimsIdentity(claims),
            IssuedAt = vencimiento.AddHours(-8),
            Expires = vencimiento,
            SigningCredentials = credenciales,
        });
    }

    private HttpClient ClienteConToken(TokenEmitido token)
    {
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return cliente;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (Directory.Exists(_rutaAlmacenamiento))
        {
            Directory.Delete(_rutaAlmacenamiento, recursive: true);
        }
    }
}
