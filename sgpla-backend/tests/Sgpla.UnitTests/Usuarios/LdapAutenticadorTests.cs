using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sgpla.Modules.Usuarios.Application.Autenticacion;
using Sgpla.Modules.Usuarios.Infrastructure.Autenticacion;

namespace Sgpla.UnitTests.Usuarios;

/// <summary>
/// Sustituye <see cref="LdapAutenticador.ConexionYBind"/> (la costura interna) para provocar cada excepción sin
/// depender de un servidor LDAP real.
/// </summary>
public sealed class LdapAutenticadorTests
{
    private readonly LdapAutenticador _autenticador = new(
        Options.Create(new LdapOptions { Servidor = "ldap-de-pruebas.invalido" }),
        NullLogger<LdapAutenticador>.Instance);

    [Fact]
    public async Task AutenticarAsync_ConBindExitoso_RespondeAutenticado()
    {
        _autenticador.ConexionYBind = (_, _, _) => { };

        var resultado = await _autenticador.AutenticarAsync("correo@uv.mx", "contrasena", TestContext.Current.CancellationToken);

        resultado.ShouldBe(ResultadoLdap.Autenticado);
    }

    [Fact]
    public async Task AutenticarAsync_ConLdapExceptionCodigo49_RespondeCredencialesInvalidas()
    {
        _autenticador.ConexionYBind = (_, _, _) => throw new LdapException(49, "Credenciales inválidas.");

        var resultado = await _autenticador.AutenticarAsync("correo@uv.mx", "mala", TestContext.Current.CancellationToken);

        resultado.ShouldBe(ResultadoLdap.CredencialesInvalidas);
    }

    [Fact]
    public async Task AutenticarAsync_ConLdapExceptionDeOtroCodigo_RespondeNoDisponible()
    {
        _autenticador.ConexionYBind = (_, _, _) => throw new LdapException(81, "Servidor no disponible.");

        var resultado = await _autenticador.AutenticarAsync("correo@uv.mx", "cualquiera", TestContext.Current.CancellationToken);

        resultado.ShouldBe(ResultadoLdap.NoDisponible);
    }

    [Fact]
    public async Task AutenticarAsync_ConDirectoryOperationExceptionDeStartTls_RespondeNoDisponible()
    {
        _autenticador.ConexionYBind = (_, _, _) => throw new DirectoryOperationException("StartTls falló.");

        var resultado = await _autenticador.AutenticarAsync("correo@uv.mx", "cualquiera", TestContext.Current.CancellationToken);

        resultado.ShouldBe(ResultadoLdap.NoDisponible);
    }

    [Fact]
    public async Task AutenticarAsync_ConTimeoutException_RespondeNoDisponible()
    {
        _autenticador.ConexionYBind = (_, _, _) => throw new TimeoutException();

        var resultado = await _autenticador.AutenticarAsync("correo@uv.mx", "cualquiera", TestContext.Current.CancellationToken);

        resultado.ShouldBe(ResultadoLdap.NoDisponible);
    }

    [Fact]
    public async Task AutenticarAsync_ConDllNotFoundException_RespondeNoDisponible()
    {
        _autenticador.ConexionYBind = (_, _, _) => throw new DllNotFoundException("libldap no encontrada.");

        var resultado = await _autenticador.AutenticarAsync("correo@uv.mx", "cualquiera", TestContext.Current.CancellationToken);

        resultado.ShouldBe(ResultadoLdap.NoDisponible);
    }

    [Fact]
    public async Task AutenticarAsync_ConTypeInitializationException_RespondeNoDisponible()
    {
        _autenticador.ConexionYBind = (_, _, _) => throw new TypeInitializationException("Tipo", new InvalidOperationException());

        var resultado = await _autenticador.AutenticarAsync("correo@uv.mx", "cualquiera", TestContext.Current.CancellationToken);

        resultado.ShouldBe(ResultadoLdap.NoDisponible);
    }

    [Fact]
    public async Task AutenticarAsync_ConOperationCanceledException_Propaga()
    {
        _autenticador.ConexionYBind = (_, _, _) => throw new OperationCanceledException();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            _autenticador.AutenticarAsync("correo@uv.mx", "cualquiera", TestContext.Current.CancellationToken));
    }
}
