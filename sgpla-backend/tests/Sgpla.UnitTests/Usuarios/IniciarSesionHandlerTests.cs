using Microsoft.Extensions.Logging.Abstractions;
using Sgpla.Modules.Usuarios.Application.Autenticacion;
using Sgpla.Modules.Usuarios.Application.Sesion;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.UnitTests.Catalogos.Articulos;

namespace Sgpla.UnitTests.Usuarios;

public sealed class IniciarSesionHandlerTests
{
    private readonly UsuarioRepositoryFalso _repositorio = new();
    private readonly AmbitosInstitucionalesFalso _ambitos = new();
    private readonly HasherContrasenasFalso _hasher = new();
    private readonly EmisorTokensFalso _emisorTokens = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();

    [Fact]
    public async Task HandleAsync_ConCuentaInexistente_FallaConCuentaNoRegistrada()
    {
        var resultado = await Handler(ResultadoLdap.Autenticado).HandleAsync(
            new IniciarSesionCommand("nadie@gmail.com", "cualquiera"), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.CuentaNoRegistrada);
    }

    [Fact]
    public async Task HandleAsync_SinArrobaEnElCorreo_AgregaUvMx()
    {
        var dgaa = Usuario.CrearDgaa("jperez@uv.mx", "Nombre", 1).Value;
        _repositorio.Registrar(1, dgaa);
        _ambitos.AreasActivas.Add(1);

        var resultado = await Handler(ResultadoLdap.Autenticado).HandleAsync(
            new IniciarSesionCommand("jperez", "cualquiera"), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_DgaaConAmbitoInactivo_FallaSinLlamarAlLdap()
    {
        var ldap = new LdapAutenticadorFalso(ResultadoLdap.Autenticado);
        var dgaa = Usuario.CrearDgaa("jperez@uv.mx", "Nombre", 1).Value;
        _repositorio.Registrar(1, dgaa);

        var resultado = await Handler(ldap).HandleAsync(
            new IniciarSesionCommand("jperez@uv.mx", "cualquiera"), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.AmbitoInactivo);
        ldap.Llamadas.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_EntidadAcademicaConAmbitoActivoYCredencialesInvalidas_Falla()
    {
        var entidad = Usuario.CrearEntidadAcademica("jperez@uv.mx", "Nombre", 5).Value;
        _repositorio.Registrar(1, entidad);
        _ambitos.EntidadesActivas.Add(5);

        var resultado = await Handler(ResultadoLdap.CredencialesInvalidas).HandleAsync(
            new IniciarSesionCommand("jperez@uv.mx", "mala"), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.CredencialesInvalidas);
    }

    [Fact]
    public async Task HandleAsync_ConLdapNoDisponible_FallaConLdapNoDisponible()
    {
        var dgaa = Usuario.CrearDgaa("jperez@uv.mx", "Nombre", 1).Value;
        _repositorio.Registrar(1, dgaa);
        _ambitos.AreasActivas.Add(1);

        var resultado = await Handler(ResultadoLdap.NoDisponible).HandleAsync(
            new IniciarSesionCommand("jperez@uv.mx", "cualquiera"), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.LdapNoDisponible);
    }

    [Fact]
    public async Task HandleAsync_SuperusuarioConCredencialDadaDeBaja_FallaConCuentaNoRegistrada()
    {
        var superusuario = Usuario.CrearSuperusuario("admin@gmail.com", "Nombre", "hash").Value;
        superusuario.DarDeBaja(DateTime.UtcNow);
        _repositorio.Registrar(1, superusuario);

        var resultado = await Handler(ResultadoLdap.Autenticado).HandleAsync(
            new IniciarSesionCommand("admin@gmail.com", "cualquiera"), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.CuentaNoRegistrada);
    }

    [Fact]
    public async Task HandleAsync_SuperusuarioConContrasenaIncorrecta_Falla()
    {
        var superusuario = Usuario.CrearSuperusuario("admin@gmail.com", "Nombre", "hash").Value;
        _repositorio.Registrar(1, superusuario);
        _hasher.Resultado = VerificacionContrasena.Incorrecta;

        var resultado = await Handler(ResultadoLdap.Autenticado).HandleAsync(
            new IniciarSesionCommand("admin@gmail.com", "mala"), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.CredencialesInvalidas);
    }

    [Fact]
    public async Task HandleAsync_SuperusuarioRequiereRehash_ActualizaElVerificadorYGuarda()
    {
        var superusuario = Usuario.CrearSuperusuario("admin@gmail.com", "Nombre", "hash-viejo").Value;
        _repositorio.Registrar(1, superusuario);
        _hasher.Resultado = VerificacionContrasena.CorrectaRequiereRehash;
        _hasher.Hasheada = "hash-nuevo";

        var resultado = await Handler(ResultadoLdap.Autenticado).HandleAsync(
            new IniciarSesionCommand("admin@gmail.com", "correcta"), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        superusuario.Credencial!.Contrasena.ShouldBe("hash-nuevo");
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_SuperusuarioConExito_NoGuardaNada()
    {
        var superusuario = Usuario.CrearSuperusuario("admin@gmail.com", "Nombre", "hash").Value;
        _repositorio.Registrar(1, superusuario);

        var resultado = await Handler(ResultadoLdap.Autenticado).HandleAsync(
            new IniciarSesionCommand("admin@gmail.com", "correcta"), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    private IniciarSesionHandler Handler(ResultadoLdap resultadoLdap) => Handler(new LdapAutenticadorFalso(resultadoLdap));

    private IniciarSesionHandler Handler(LdapAutenticadorFalso ldap) => new(
        _repositorio, _ambitos, ldap, _hasher, _emisorTokens, _unidadDeTrabajo, NullLogger<IniciarSesionHandler>.Instance);
}
