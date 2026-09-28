using Microsoft.Extensions.Logging.Abstractions;
using Sgpla.Modules.Usuarios.Application.Cuentas;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.UnitTests.Catalogos.Articulos;
using Sgpla.UnitTests.Institucional;

namespace Sgpla.UnitTests.Usuarios;

public sealed class DarDeBajaCuentaHandlerTests
{
    private const int UsuarioActualId = 1;
    private const int OtroId = 2;

    private readonly UsuarioRepositoryFalso _repositorio = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();

    [Fact]
    public async Task HandleAsync_ConLaPropiaCuenta_FallaConBajaPropia()
    {
        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaCuentaCommand(UsuarioActualId), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.BajaPropia);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConCuentaInexistente_FallaConNoEncontrado()
    {
        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaCuentaCommand(OtroId), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.NoEncontrado(OtroId));
    }

    [Fact]
    public async Task HandleAsync_ConUltimoSuperusuario_FallaConUltimoSuperusuarioSinGuardar()
    {
        var superusuario = Usuario.CrearSuperusuario("admin@gmail.com", "Nombre", "hash").Value;
        _repositorio.Registrar(OtroId, superusuario);
        _repositorio.Superusuarios = 1;

        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaCuentaCommand(OtroId), TestContext.Current.CancellationToken);

        resultado.Error.ShouldBe(UsuarioErrors.UltimoSuperusuario);
        _unidadDeTrabajo.Guardados.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_ConSuperusuarioNoUltimo_DaDeBajaLaCuentaYLaCredencialConElMismoInstante()
    {
        var superusuario = Usuario.CrearSuperusuario("admin@gmail.com", "Nombre", "hash").Value;
        _repositorio.Registrar(OtroId, superusuario);
        _repositorio.Superusuarios = 2;
        var ahoraConMilisegundos = new DateTimeOffset(2026, 3, 15, 10, 30, 45, 250, TimeSpan.Zero);

        var resultado = await Handler(ahoraConMilisegundos).HandleAsync(
            new DarDeBajaCuentaCommand(OtroId), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        var instanteEsperado = new DateTime(2026, 3, 15, 10, 30, 45, DateTimeKind.Utc);
        superusuario.FechaEliminacion.ShouldBe(instanteEsperado);
        superusuario.Credencial!.FechaEliminacion.ShouldBe(instanteEsperado);
        _unidadDeTrabajo.Guardados.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ConCuentaDgaa_DaDeBaja()
    {
        var dgaa = Usuario.CrearDgaa("jperez@uv.mx", "Nombre", 1).Value;
        _repositorio.Registrar(OtroId, dgaa);

        var resultado = await Handler(DateTimeOffset.UtcNow).HandleAsync(
            new DarDeBajaCuentaCommand(OtroId), TestContext.Current.CancellationToken);

        resultado.IsSuccess.ShouldBeTrue();
        dgaa.FechaEliminacion.ShouldNotBeNull();
    }

    private DarDeBajaCuentaHandler Handler(DateTimeOffset ahora) => new(
        _repositorio,
        new CurrentUserFalso(UsuarioActualId),
        _unidadDeTrabajo,
        new TimeProviderFalso(ahora),
        NullLogger<DarDeBajaCuentaHandler>.Instance);
}
