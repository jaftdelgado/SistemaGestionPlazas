using System.Net;
using Sgpla.IntegrationTests.Infraestructura;
using Sgpla.IntegrationTests.OfertaEducativa;

namespace Sgpla.IntegrationTests.SolicitudesApertura;

public sealed class PeriodoSolicitudEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private readonly SgplaApiFactory _api = new(sqlServer);

    [Fact]
    public async Task ObtenerPeriodos_DevuelveParejaConfiguradaParaLosTresRoles()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        using var dgaa = await escenario.Oferta.Dgaa.GetAsync(Ruta, TestContext.Current.CancellationToken);
        using var entidad = await escenario.Entidad.GetAsync(Ruta, TestContext.Current.CancellationToken);
        using var superusuario = await escenario.Superusuario.GetAsync(Ruta, TestContext.Current.CancellationToken);

        await VerificaParejaAsync(dgaa, escenario.PeriodoActualId, escenario.PeriodoSiguienteId);
        await VerificaParejaAsync(entidad, escenario.PeriodoActualId, escenario.PeriodoSiguienteId);
        await VerificaParejaAsync(superusuario, escenario.PeriodoActualId, escenario.PeriodoSiguienteId);
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static readonly Uri Ruta = new("/api/v1/solicitudes-apertura/periodos", UriKind.Relative);

    private static async Task VerificaParejaAsync(HttpResponseMessage respuesta, int actualId, int siguienteId)
    {
        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var contenido = await EscenarioOferta.Leer(respuesta);
        var actual = contenido.GetProperty("actual");
        var siguiente = contenido.GetProperty("siguiente");

        actual.GetProperty("clave").GetString().ShouldBe(SqlServerFixture.ClavePeriodoActual);
        actual.GetProperty("id").GetInt32().ShouldBe(actualId);
        actual.GetProperty("fechaInicio").GetString().ShouldBe("2026-02-02");
        actual.GetProperty("fechaFin").GetString().ShouldBe("2026-07-10");
        siguiente.GetProperty("clave").GetString().ShouldBe(SqlServerFixture.ClavePeriodoSiguiente);
        siguiente.GetProperty("id").GetInt32().ShouldBe(siguienteId);
        siguiente.GetProperty("fechaInicio").GetString().ShouldBe("2026-08-10");
        siguiente.GetProperty("fechaFin").GetString().ShouldBe("2027-01-22");
    }
}
