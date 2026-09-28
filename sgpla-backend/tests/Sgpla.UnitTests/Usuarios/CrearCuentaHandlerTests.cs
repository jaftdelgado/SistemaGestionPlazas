using Microsoft.Extensions.Logging.Abstractions;
using Sgpla.Modules.Usuarios.Application.Cuentas;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.SharedKernel;
using Sgpla.UnitTests.Catalogos.Articulos;

namespace Sgpla.UnitTests.Usuarios;

public sealed class CrearCuentaHandlerTests
{
    private readonly UsuarioRepositoryFalso _repositorio = new();
    private readonly AmbitosInstitucionalesFalso _ambitos = new();
    private readonly HasherContrasenasFalso _hasher = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();

    [Fact]
    public async Task HandleAsync_Superusuario_CreaLaCuentaConTemporal()
    {
        var resultado = await Handler("Tmp0ral!23").HandleAsync(
            new CrearCuentaCommand("admin@gmail.com", "Nombre", (byte)Rol.Superusuario, null, null),
            TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.ContrasenaTemporal.ShouldBe("Tmp0ral!23");
        _repositorio.Agregados.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task HandleAsync_Dgaa_CreaLaCuentaSinTemporal()
    {
        _ambitos.AreasActivas.Add(1);

        var resultado = await Handler("Tmp0ral!23").HandleAsync(
            new CrearCuentaCommand("jperez@uv.mx", "Nombre", (byte)Rol.Dgaa, 1, null),
            TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.ContrasenaTemporal.ShouldBeNull();
        _repositorio.Agregados.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task HandleAsync_EntidadAcademica_CreaLaCuentaSinTemporal()
    {
        _ambitos.EntidadesActivas.Add(5);

        var resultado = await Handler("Tmp0ral!23").HandleAsync(
            new CrearCuentaCommand("jperez@uv.mx", "Nombre", (byte)Rol.EntidadAcademica, null, 5),
            TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.ContrasenaTemporal.ShouldBeNull();
        _repositorio.Agregados.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task HandleAsync_DgaaConAreaInactiva_FallaConAreaAcademicaInexistenteSinAgregar()
    {
        var resultado = await Handler("Tmp0ral!23").HandleAsync(
            new CrearCuentaCommand("jperez@uv.mx", "Nombre", (byte)Rol.Dgaa, 1, null),
            TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.AreaAcademicaInexistente);
        _repositorio.Agregados.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_EntidadAcademicaConEntidadInactiva_FallaConEntidadAcademicaInexistenteSinAgregar()
    {
        var resultado = await Handler("Tmp0ral!23").HandleAsync(
            new CrearCuentaCommand("jperez@uv.mx", "Nombre", (byte)Rol.EntidadAcademica, null, 5),
            TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.EntidadAcademicaInexistente);
        _repositorio.Agregados.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ConCorreoYaEnUso_FallaConCorreoDuplicadoSinAgregar()
    {
        _repositorio.CorreosExistentes.Add("admin@gmail.com");

        var resultado = await Handler("Tmp0ral!23").HandleAsync(
            new CrearCuentaCommand("admin@gmail.com", "Nombre", (byte)Rol.Superusuario, null, null),
            TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.CorreoDuplicado);
        _repositorio.Agregados.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_SuperusuarioConCorreoInvalido_NoLlamaAlHasher()
    {
        var resultado = await Handler("Tmp0ral!23").HandleAsync(
            new CrearCuentaCommand("", "Nombre", (byte)Rol.Superusuario, null, null),
            TestContext.Current.CancellationToken);

        resultado.IsFailure.ShouldBeTrue();
        _hasher.Hasheados.ShouldBeEmpty();
    }

    private CrearCuentaHandler Handler(string temporal) => new(
        _repositorio,
        _ambitos,
        _hasher,
        new GeneradorContrasenasFalso(temporal),
        new CurrentUserFalso(1),
        _unidadDeTrabajo,
        NullLogger<CrearCuentaHandler>.Instance);
}
