using Microsoft.Extensions.Logging.Abstractions;
using Sgpla.Modules.Usuarios.Application.Cuentas;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.UnitTests.Catalogos.Articulos;

namespace Sgpla.UnitTests.Usuarios;

public sealed class CrearSuperusuarioInicialHandlerTests
{
    private readonly UsuarioRepositoryFalso _repositorio = new();
    private readonly HasherContrasenasFalso _hasher = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();

    [Fact]
    public async Task HandleAsync_SinSuperusuarioActivo_CreaLaCuenta()
    {
        var handler = Handler("Tmp0ral!23");

        var resultado = await handler.HandleAsync(
            new CrearSuperusuarioInicialCommand("admin@gmail.com", "Superusuario"), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.ContrasenaTemporal.ShouldBe("Tmp0ral!23");
        _repositorio.Agregados.ShouldHaveSingleItem();
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConSuperusuarioActivo_FallaSinCrearNada()
    {
        _repositorio.Superusuarios = 1;
        var handler = Handler("Tmp0ral!23");

        var resultado = await handler.HandleAsync(
            new CrearSuperusuarioInicialCommand("admin@gmail.com", "Superusuario"), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.SuperusuarioExistente);
        _repositorio.Agregados.ShouldBeEmpty();
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConCorreoYaEnUso_FallaConCorreoDuplicado()
    {
        _repositorio.CorreosExistentes.Add("admin@gmail.com");
        var handler = Handler("Tmp0ral!23");

        var resultado = await handler.HandleAsync(
            new CrearSuperusuarioInicialCommand("admin@gmail.com", "Superusuario"), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.CorreoDuplicado);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConCorreoInvalido_NoLlamaAlHasher()
    {
        var handler = Handler("Tmp0ral!23");

        var resultado = await handler.HandleAsync(
            new CrearSuperusuarioInicialCommand("", "Superusuario"), TestContext.Current.CancellationToken);

        resultado.IsFailure.ShouldBeTrue();
        _hasher.Hasheados.ShouldBeEmpty();
    }

    private CrearSuperusuarioInicialHandler Handler(string temporal) => new(
        _repositorio, _hasher, new GeneradorContrasenasFalso(temporal), _unidadDeTrabajo,
        NullLogger<CrearSuperusuarioInicialHandler>.Instance);
}
