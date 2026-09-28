using Microsoft.Extensions.Logging.Abstractions;
using Sgpla.Modules.Usuarios.Application.Cuentas;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.UnitTests.Catalogos.Articulos;

namespace Sgpla.UnitTests.Usuarios;

public sealed class RestablecerContrasenaHandlerTests
{
    private const int UsuarioActualId = 1;
    private const int OtroId = 2;

    private readonly UsuarioRepositoryFalso _repositorio = new();
    private readonly HasherContrasenasFalso _hasher = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();

    [Fact]
    public async Task HandleAsync_ConLaPropiaCuenta_FallaConRestablecimientoPropio()
    {
        var resultado = await Handler("Tmp0ral!23").HandleAsync(
            new RestablecerContrasenaCommand(UsuarioActualId), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.RestablecimientoPropio);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConCuentaInexistente_FallaConNoEncontrado()
    {
        var resultado = await Handler("Tmp0ral!23").HandleAsync(
            new RestablecerContrasenaCommand(OtroId), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.NoEncontrado(OtroId));
    }

    [Fact]
    public async Task HandleAsync_ConCuentaQueNoEsSuperusuario_FallaConRestablecimientoNoAplica()
    {
        var dgaa = Usuario.CrearDgaa("jperez@uv.mx", "Nombre", 1).Value;
        _repositorio.Registrar(OtroId, dgaa);

        var resultado = await Handler("Tmp0ral!23").HandleAsync(
            new RestablecerContrasenaCommand(OtroId), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.RestablecimientoNoAplica);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConSuperusuario_RegresaLaCredencialAPendienteYDevuelveLaTemporal()
    {
        var superusuario = Usuario.CrearSuperusuario("admin@gmail.com", "Nombre", "hash").Value;
        superusuario.EstablecerContrasena("hash-anterior", DateTime.UtcNow);
        _repositorio.Registrar(OtroId, superusuario);

        var resultado = await Handler("Tmp0ral!23").HandleAsync(
            new RestablecerContrasenaCommand(OtroId), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.ShouldBe("Tmp0ral!23");
        superusuario.Credencial!.FechaActualizacion.ShouldBeNull();
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    private RestablecerContrasenaHandler Handler(string temporal) => new(
        _repositorio,
        _hasher,
        new GeneradorContrasenasFalso(temporal),
        new CurrentUserFalso(UsuarioActualId),
        _unidadDeTrabajo,
        NullLogger<RestablecerContrasenaHandler>.Instance);
}
