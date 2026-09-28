using Microsoft.Extensions.DependencyInjection;
using Sgpla.IntegrationTests.Infraestructura;
using Sgpla.Modules.Usuarios;
using Sgpla.Modules.Usuarios.Application.Cuentas;

namespace Sgpla.IntegrationTests.Usuarios;

/// <summary>
/// La creación del primer Superusuario la cubre <c>CrearSuperusuarioInicialHandlerTests</c> (unitaria): la base
/// compartida de estas pruebas siempre tiene Superusuarios, así que aquí solo se cubre la ejecución repetida.
/// </summary>
public sealed class BootstrapTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private readonly SgplaApiFactory _api = new(sqlServer);

    [Fact]
    public async Task EjecutarBootstrapAsync_ConUnSuperusuarioActivo_Devuelve0YNoCreaNada()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var existiaAntes = await ExisteCorreoDeBootstrapAsync();
        using var salida = new StringWriter();

        var codigo = await UsuariosModule.EjecutarBootstrapAsync(_api.Services, salida, TestContext.Current.CancellationToken);

        codigo.ShouldBe(0);
        salida.ToString().ShouldContain("Ya existe un Superusuario activo");
        existiaAntes.ShouldBeFalse();
        (await ExisteCorreoDeBootstrapAsync()).ShouldBeFalse();
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private async Task<bool> ExisteCorreoDeBootstrapAsync()
    {
        await using var alcance = _api.Services.CreateAsyncScope();
        var repositorio = alcance.ServiceProvider.GetRequiredService<IUsuarioRepository>();
        return await repositorio.ExisteCorreoAsync(SgplaApiFactory.CorreoDeBootstrap, TestContext.Current.CancellationToken);
    }
}
