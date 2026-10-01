using System.Net;
using System.Net.Http.Json;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests.SolicitudesApertura;

public sealed class ReferenciasSolicitudesAperturaTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private const string RutaExperiencias = "/api/v1/oferta-educativa/experiencias-educativas";
    private const string RutaPlanes = "/api/v1/oferta-educativa/planes-estudio";
    private const string RutaPeriodos = "/api/v1/oferta-educativa/periodos-escolares";

    private readonly SgplaApiFactory _api = new(sqlServer);

    [Theory]
    [InlineData("PENDIENTE", HttpStatusCode.Conflict)]
    [InlineData("ACEPTADA", HttpStatusCode.Conflict)]
    [InlineData("RECHAZADA", HttpStatusCode.NoContent)]
    [InlineData("CANCELADA", HttpStatusCode.NoContent)]
    public async Task BajaDeExperiencia_DependeDelEstadoDeLaSolicitud(string estado, HttpStatusCode esperado)
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        await DatosSolicitudesSql.InsertarAsync(
            sqlServer.CadenaConexion, escenario.ExperienciaId, escenario.PeriodoSiguienteId, estado, "A1");

        using var respuesta = await escenario.Oferta.Dgaa.DeleteAsync(
            new Uri($"{RutaExperiencias}/{escenario.ExperienciaId}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        respuesta.StatusCode.ShouldBe(esperado);
        if (esperado == HttpStatusCode.Conflict)
        {
            var problema = await Sgpla.IntegrationTests.OfertaEducativa.EscenarioOferta.Leer(respuesta);
            problema.GetProperty("codigo").GetString().ShouldBe("ExperienciaEducativa.TieneReferencias");
        }
    }

    [Theory]
    [InlineData("PENDIENTE", HttpStatusCode.Conflict)]
    [InlineData("ACEPTADA", HttpStatusCode.Conflict)]
    [InlineData("RECHAZADA", HttpStatusCode.NoContent)]
    [InlineData("CANCELADA", HttpStatusCode.NoContent)]
    public async Task BajaDePlan_DependeDelEstadoDeLaSolicitud(string estado, HttpStatusCode esperado)
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        await DatosSolicitudesSql.InsertarAsync(
            sqlServer.CadenaConexion, escenario.ExperienciaId, escenario.PeriodoSiguienteId, estado, "A1");

        using var respuesta = await escenario.Oferta.Dgaa.DeleteAsync(
            new Uri($"{RutaPlanes}/{escenario.PlanId}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        respuesta.StatusCode.ShouldBe(esperado);
        if (esperado == HttpStatusCode.Conflict)
        {
            var problema = await Sgpla.IntegrationTests.OfertaEducativa.EscenarioOferta.Leer(respuesta);
            problema.GetProperty("codigo").GetString().ShouldBe("ExperienciaEducativa.TieneReferencias");
        }
    }

    [Fact]
    public async Task BajaDePeriodo_SeBloqueaAunqueLaSolicitudEsteCancelada()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        using var respuestaCrearPeriodo = await escenario.Superusuario.PostAsJsonAsync(
            new Uri(RutaPeriodos, UriKind.Relative),
            new { clave = DatosUnicos.ClavePeriodo(), fechaInicio = "2026-08-10", fechaFin = "2027-01-22" },
            TestContext.Current.CancellationToken);
        respuestaCrearPeriodo.StatusCode.ShouldBe(HttpStatusCode.Created);
        var periodoId = (await Sgpla.IntegrationTests.OfertaEducativa.EscenarioOferta.Leer(respuestaCrearPeriodo))
            .GetProperty("id").GetInt32();
        await DatosSolicitudesSql.InsertarAsync(
            sqlServer.CadenaConexion, escenario.ExperienciaId, periodoId, "CANCELADA", "A1");

        using var baja = await escenario.Superusuario.DeleteAsync(
            new Uri($"{RutaPeriodos}/{periodoId}", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var problema = await Sgpla.IntegrationTests.OfertaEducativa.EscenarioOferta.Leer(baja);

        baja.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("PeriodoEscolar.TieneReferencias");
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();
}
