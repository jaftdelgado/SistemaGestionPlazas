using Microsoft.Extensions.Logging.Abstractions;
using Sgpla.Modules.Usuarios.Application.Autenticacion;
using Sgpla.Modules.Usuarios.Application.Sesion;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.UnitTests.Catalogos.Articulos;
using Sgpla.UnitTests.Institucional;

namespace Sgpla.UnitTests.Usuarios;

public sealed class CambiarContrasenaHandlerTests
{
    private readonly UsuarioRepositoryFalso _repositorio = new();
    private readonly HasherContrasenasFalso _hasher = new();
    private readonly EmisorTokensFalso _emisorTokens = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();

    [Fact]
    public async Task HandleAsync_CuentaInexistente_FallaConCuentaNoRegistrada()
    {
        var resultado = await Handler(1).HandleAsync(
            new CambiarContrasenaCommand("actual", "Nueva123!"), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.CuentaNoRegistrada);
    }

    [Fact]
    public async Task HandleAsync_CuentaQueNoEsSuperusuario_FallaConCambioContrasenaNoAplica()
    {
        var dgaa = Usuario.CrearDgaa("jperez@uv.mx", "Nombre", 1).Value;
        _repositorio.Registrar(1, dgaa);

        var resultado = await Handler(1).HandleAsync(
            new CambiarContrasenaCommand("actual", "Nueva123!"), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.CambioContrasenaNoAplica);
    }

    [Fact]
    public async Task HandleAsync_ContrasenaActualIncorrecta_Falla()
    {
        var superusuario = Usuario.CrearSuperusuario("admin@gmail.com", "Nombre", "hash").Value;
        _repositorio.Registrar(1, superusuario);
        _hasher.Resultado = VerificacionContrasena.Incorrecta;

        var resultado = await Handler(1).HandleAsync(
            new CambiarContrasenaCommand("mala", "Nueva123!"), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.ContrasenaActualIncorrecta);
    }

    [Fact]
    public async Task HandleAsync_ContrasenaNuevaDebil_FallaConElErrorDeLaPolitica()
    {
        var superusuario = Usuario.CrearSuperusuario("admin@gmail.com", "Nombre", "hash").Value;
        _repositorio.Registrar(1, superusuario);

        var resultado = await Handler(1).HandleAsync(
            new CambiarContrasenaCommand("actual", "debil"), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.ContrasenaLongitudInvalida);
    }

    [Fact]
    public async Task HandleAsync_ContrasenaNuevaIgualALaActual_Falla()
    {
        var superusuario = Usuario.CrearSuperusuario("admin@gmail.com", "Nombre", "hash").Value;
        _repositorio.Registrar(1, superusuario);

        var resultado = await Handler(1).HandleAsync(
            new CambiarContrasenaCommand("Igual123!", "Igual123!"), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.ContrasenaNuevaIgualActual);
    }

    [Fact]
    public async Task HandleAsync_ConExito_TruncaElInstanteASegundosYGuarda()
    {
        var superusuario = Usuario.CrearSuperusuario("admin@gmail.com", "Nombre", "hash").Value;
        _repositorio.Registrar(1, superusuario);
        var conMilisegundos = new DateTimeOffset(new DateTime(2026, 1, 1, 10, 0, 0, 500, DateTimeKind.Utc));

        var resultado = await Handler(1, conMilisegundos).HandleAsync(
            new CambiarContrasenaCommand("actual", "Nueva123!"), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        superusuario.Credencial!.FechaActualizacion.ShouldBe(new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc));
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    private CambiarContrasenaHandler Handler(int usuarioId, DateTimeOffset? ahora = null) => new(
        new CurrentUserFalso(usuarioId),
        _repositorio,
        _hasher,
        _emisorTokens,
        _unidadDeTrabajo,
        new TimeProviderFalso(ahora ?? DateTimeOffset.UtcNow),
        NullLogger<CambiarContrasenaHandler>.Instance);
}
