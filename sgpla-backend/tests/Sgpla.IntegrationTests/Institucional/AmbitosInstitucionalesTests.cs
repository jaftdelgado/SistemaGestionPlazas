using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Sgpla.IntegrationTests.Infraestructura;
using Sgpla.Modules.Institucional.Application.Contracts;

namespace Sgpla.IntegrationTests.Institucional;

/// <summary>
/// <see cref="IAmbitosInstitucionales"/> es un contrato público, así que se resuelve directamente del contenedor
/// del host (sin pasar por HTTP) y se ejerce contra SQL Server real, con las áreas y entidades que crean las
/// pruebas de sus propios endpoints.
/// </summary>
public sealed class AmbitosInstitucionalesTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AreaAcademicaActivaAsync_ActivaInexistenteYDadaDeBaja_RespondeSegunCorresponda()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var (idActiva, _) = await CrearArea(cliente);
        var (idBaja, _) = await CrearArea(cliente);
        await DarDeBajaArea(cliente, idBaja);

        (await ConAmbitosAsync(a => a.AreaAcademicaActivaAsync(idActiva, Cancelacion))).ShouldBeTrue();
        (await ConAmbitosAsync(a => a.AreaAcademicaActivaAsync(idBaja, Cancelacion))).ShouldBeFalse();
        (await ConAmbitosAsync(a => a.AreaAcademicaActivaAsync(int.MaxValue, Cancelacion))).ShouldBeFalse();
    }

    [Fact]
    public async Task EntidadAcademicaActivaAsync_ActivaInexistenteYDadaDeBaja_RespondeSegunCorresponda()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var (areaId, _) = await CrearArea(cliente);
        var idActiva = await CrearEntidad(cliente, areaId);
        var idBaja = await CrearEntidad(cliente, areaId);
        await DarDeBajaEntidad(cliente, idBaja);

        (await ConAmbitosAsync(a => a.EntidadAcademicaActivaAsync(idActiva, Cancelacion))).ShouldBeTrue();
        (await ConAmbitosAsync(a => a.EntidadAcademicaActivaAsync(idBaja, Cancelacion))).ShouldBeFalse();
        (await ConAmbitosAsync(a => a.EntidadAcademicaActivaAsync(int.MaxValue, Cancelacion))).ShouldBeFalse();
    }

    [Fact]
    public async Task ObtenerAreasAsync_IncluyeLasDadasDeBajaYOmiteLasInexistentes()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var (idActiva, claveActiva) = await CrearArea(cliente);
        var (idBaja, claveBaja) = await CrearArea(cliente);
        await DarDeBajaArea(cliente, idBaja);

        var resumenes = await ConAmbitosAsync(a => a.ObtenerAreasAsync([idActiva, idBaja, int.MaxValue], Cancelacion));

        resumenes.Keys.ShouldBe([idActiva, idBaja], ignoreOrder: true);
        resumenes[idActiva].Clave.ShouldBe(claveActiva);
        resumenes[idBaja].Clave.ShouldBe(claveBaja);
    }

    [Fact]
    public async Task ObtenerAreasAsync_ConColeccionVacia_DevuelveDiccionarioVacio()
    {
        var resumenes = await ConAmbitosAsync(a => a.ObtenerAreasAsync([], Cancelacion));

        resumenes.ShouldBeEmpty();
    }

    [Fact]
    public async Task ObtenerEntidadesAsync_IncluyeLasDadasDeBajaYOmiteLasInexistentes()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var (areaId, _) = await CrearArea(cliente);
        var idActiva = await CrearEntidad(cliente, areaId);
        var idBaja = await CrearEntidad(cliente, areaId);
        await DarDeBajaEntidad(cliente, idBaja);

        var resumenes = await ConAmbitosAsync(a => a.ObtenerEntidadesAsync([idActiva, idBaja, int.MaxValue], Cancelacion));

        resumenes.Keys.ShouldBe([idActiva, idBaja], ignoreOrder: true);
    }

    [Fact]
    public async Task ObtenerEntidadesAsync_DevuelveElAreaAcademicaDeCadaEntidad()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var (areaId, _) = await CrearArea(cliente);
        var (otraAreaId, _) = await CrearArea(cliente);
        var idEnArea = await CrearEntidad(cliente, areaId);
        var idEnOtraArea = await CrearEntidad(cliente, otraAreaId);

        var resumenes = await ConAmbitosAsync(a => a.ObtenerEntidadesAsync([idEnArea, idEnOtraArea], Cancelacion));

        resumenes[idEnArea].AreaAcademicaId.ShouldBe(areaId);
        resumenes[idEnOtraArea].AreaAcademicaId.ShouldBe(otraAreaId);
    }

    [Fact]
    public async Task ObtenerEntidadesAsync_ConColeccionVacia_DevuelveDiccionarioVacio()
    {
        var resumenes = await ConAmbitosAsync(a => a.ObtenerEntidadesAsync([], Cancelacion));

        resumenes.ShouldBeEmpty();
    }

    [Fact]
    public async Task ObtenerEntidadesDeAreaAsync_IncluyeLasDadasDeBajaYExcluyeLasDeOtraArea()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var (areaId, _) = await CrearArea(cliente);
        var (otraAreaId, _) = await CrearArea(cliente);
        var idActiva = await CrearEntidad(cliente, areaId);
        var idBaja = await CrearEntidad(cliente, areaId);
        await DarDeBajaEntidad(cliente, idBaja);
        var idDeOtraArea = await CrearEntidad(cliente, otraAreaId);

        var ids = await ConAmbitosAsync(a => a.ObtenerEntidadesDeAreaAsync(areaId, Cancelacion));

        ids.ShouldContain(idActiva);
        ids.ShouldContain(idBaja);
        ids.ShouldNotContain(idDeOtraArea);
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    /// <summary>
    /// Resuelve <see cref="IAmbitosInstitucionales"/> en un alcance propio y lo mantiene vivo mientras se ejecuta
    /// <paramref name="accion"/>: el contrato depende de <c>SgplaDbContext</c>, que el alcance dispone al salir.
    /// </summary>
    private async Task<T> ConAmbitosAsync<T>(Func<IAmbitosInstitucionales, Task<T>> accion)
    {
        await using var alcance = _api.Services.CreateAsyncScope();
        return await accion(alcance.ServiceProvider.GetRequiredService<IAmbitosInstitucionales>());
    }

    private static async Task<(int Id, int Clave)> CrearArea(HttpClient cliente)
    {
        var clave = DatosUnicos.ClaveEntera();
        using var respuesta = await cliente.PostAsJsonAsync(
            new Uri("/api/v1/institucional/areas-academicas", UriKind.Relative),
            new { clave, nombre = "Facultad de Prueba", telefono = "2288421700", extension = (string?)null },
            Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        var creada = await respuesta.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(Cancelacion);
        return (creada.GetProperty("id").GetInt32(), clave);
    }

    private static Task<HttpResponseMessage> DarDeBajaArea(HttpClient cliente, int id) =>
        cliente.DeleteAsync(new Uri($"/api/v1/institucional/areas-academicas/{id}", UriKind.Relative), Cancelacion);

    private static async Task<int> CrearEntidad(HttpClient cliente, int areaAcademicaId)
    {
        using var respuesta = await cliente.PostAsJsonAsync(
            new Uri("/api/v1/institucional/entidades-academicas", UriKind.Relative),
            new
            {
                clave = DatosUnicos.ClaveAlfanumerica(),
                nombre = "Facultad de Prueba",
                calle = "Calle de prueba",
                numeroExterior = (string?)null,
                colonia = "Colonia de prueba",
                codigoPostal = "91020",
                telefono = "2288421700",
                extension = (string?)null,
                campusId = 1,
                areaAcademicaId,
                municipioId = 87,
            },
            Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        var creada = await respuesta.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(Cancelacion);
        return creada.GetProperty("id").GetInt32();
    }

    private static Task<HttpResponseMessage> DarDeBajaEntidad(HttpClient cliente, int id) =>
        cliente.DeleteAsync(new Uri($"/api/v1/institucional/entidades-academicas/{id}", UriKind.Relative), Cancelacion);
}
