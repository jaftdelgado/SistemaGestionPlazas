using Microsoft.Extensions.DependencyInjection;
using Sgpla.IntegrationTests.Infraestructura;
using Sgpla.Modules.Catalogos.Application.Contracts;

namespace Sgpla.IntegrationTests.Catalogos;

/// <summary>
/// <see cref="IClasificacionesAcademicas"/> es un contrato público, así que se resuelve directamente del contenedor
/// del host (sin pasar por HTTP) y se ejerce contra los valores de la semilla.
/// </summary>
public sealed class ClasificacionesAcademicasContratoTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ObtenerSistemasEducativosAsync_DevuelveLosExistentesYOmiteLosInexistentes()
    {
        var resumenes = await ConClasificacionesAsync(c => c.ObtenerSistemasEducativosAsync([1, int.MaxValue], Cancelacion));

        resumenes.Keys.ShouldBe([1]);
        resumenes[1].ShouldBe(new SistemaEducativoResumen(1, "Escolarizada"));
    }

    [Fact]
    public async Task ObtenerNivelesFormacionAsync_DevuelveLosExistentesYOmiteLosInexistentes()
    {
        var resumenes = await ConClasificacionesAsync(c => c.ObtenerNivelesFormacionAsync([3, int.MaxValue], Cancelacion));

        resumenes.Keys.ShouldBe([3]);
        resumenes[3].ShouldBe(new NivelFormacionResumen(3, "LIC", "Licenciatura"));
    }

    [Fact]
    public async Task ObtenerAreasFormacionAsync_DevuelveLasExistentesYOmiteLasInexistentes()
    {
        var resumenes = await ConClasificacionesAsync(c => c.ObtenerAreasFormacionAsync([2, int.MaxValue], Cancelacion));

        resumenes.Keys.ShouldBe([2]);
        resumenes[2].ShouldBe(new AreaFormacionResumen(2, "112", "Área de Formación Disciplinaria"));
    }

    [Fact]
    public async Task Obtener_ConColeccionVacia_DevuelveDiccionariosVacios()
    {
        (await ConClasificacionesAsync(c => c.ObtenerSistemasEducativosAsync([], Cancelacion))).ShouldBeEmpty();
        (await ConClasificacionesAsync(c => c.ObtenerNivelesFormacionAsync([], Cancelacion))).ShouldBeEmpty();
        (await ConClasificacionesAsync(c => c.ObtenerAreasFormacionAsync([], Cancelacion))).ShouldBeEmpty();
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    /// <summary>
    /// Resuelve <see cref="IClasificacionesAcademicas"/> en un alcance propio y lo mantiene vivo mientras se ejecuta
    /// <paramref name="accion"/>: el contrato depende de <c>SgplaDbContext</c>, que el alcance dispone al salir.
    /// </summary>
    private async Task<T> ConClasificacionesAsync<T>(Func<IClasificacionesAcademicas, Task<T>> accion)
    {
        await using var alcance = _api.Services.CreateAsyncScope();
        return await accion(alcance.ServiceProvider.GetRequiredService<IClasificacionesAcademicas>());
    }
}
