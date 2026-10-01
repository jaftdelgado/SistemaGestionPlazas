using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.IntegrationTests.Infraestructura;
using Sgpla.IntegrationTests.OfertaEducativa;
using Sgpla.IntegrationTests.SolicitudesApertura;

namespace Sgpla.IntegrationTests;

/// <summary>
/// La concurrencia optimista: si la fila cambia entre que se lee y que se guarda, la segunda operación responde 409 y
/// no pisa el cambio. Las pruebas de integración no ven los tipos internos de los módulos, así que el choque se
/// provoca con un <see cref="IUnitOfWork"/> de prueba que actualiza la fila por SQL justo antes de guardar.
/// </summary>
public sealed class ConcurrenciaTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private readonly SgplaApiFactory _api = new(sqlServer);

    [Fact]
    public async Task DbUpdateConcurrencyException_Responde409ConCodigo()
    {
        var manejador = ObtenerManejador();
        await using var alcance = _api.Services.CreateAsyncScope();
        var contexto = new DefaultHttpContext { RequestServices = alcance.ServiceProvider };
        contexto.Response.Body = new MemoryStream();

        var manejada = await manejador.TryHandleAsync(
            contexto, new DbUpdateConcurrencyException("Fila modificada."), TestContext.Current.CancellationToken);

        manejada.ShouldBeTrue();
        contexto.Response.StatusCode.ShouldBe((int)HttpStatusCode.Conflict);
        contexto.Response.Body.Position = 0;
        var problema = await JsonSerializer.DeserializeAsync<JsonElement>(
            contexto.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
        problema.GetProperty("codigo").GetString().ShouldBe(ConcurrenciaExceptionHandler.Codigo);
        problema.GetProperty("codigo").GetString().ShouldBe("Persistencia.ModificacionConcurrente");
    }

    [Fact]
    public async Task OtraExcepcion_NoLaManeja()
    {
        var manejada = await ObtenerManejador().TryHandleAsync(
            new DefaultHttpContext(), new InvalidOperationException(), TestContext.Current.CancellationToken);

        manejada.ShouldBeFalse();
    }

    [Fact]
    public async Task Aceptar_ConLaFilaCambiadaAntesDeGuardar_Devuelve409YLaSolicitudSigueEnPendiente()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (solicitudId, _) = await escenario.CrearSolicitudAsync();
        using var host = _api.WithWebHostBuilder(builder => builder.ConfigureTestServices(servicios =>
            servicios.AddScoped<IUnitOfWork>(proveedor => new UnitOfWorkConCambioConcurrente(
                proveedor.GetRequiredService<SgplaDbContext>(), sqlServer.CadenaConexion, solicitudId))));
        using var cliente = host.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = escenario.Oferta.Dgaa.DefaultRequestHeaders.Authorization;

        using var respuesta = await cliente.PostAsJsonAsync(
            new Uri($"/api/v1/solicitudes-apertura/solicitudes/{solicitudId}/aceptar", UriKind.Relative),
            new { comentarios = (string?)null },
            TestContext.Current.CancellationToken);
        var problema = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("Persistencia.ModificacionConcurrente");
        using var obtenida = await escenario.Oferta.Dgaa.GetAsync(
            new Uri($"/api/v1/solicitudes-apertura/solicitudes/{solicitudId}", UriKind.Relative),
            TestContext.Current.CancellationToken);
        obtenida.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await EscenarioOferta.Leer(obtenida)).GetProperty("estado").GetString().ShouldBe("PENDIENTE");
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private ConcurrenciaExceptionHandler ObtenerManejador() =>
        _api.Services.GetServices<IExceptionHandler>().OfType<ConcurrenciaExceptionHandler>().Single();

    /// <summary>Simula a otro usuario: renueva el <c>rowversion</c> de la solicitud justo antes de guardar.</summary>
    private sealed class UnitOfWorkConCambioConcurrente(SgplaDbContext contexto, string cadenaConexion, int solicitudId)
        : IUnitOfWork
    {
        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await using (var conexion = new SqlConnection(cadenaConexion))
            {
                await conexion.OpenAsync(cancellationToken);
                await using var comando = conexion.CreateCommand();
                comando.CommandText = "UPDATE academico.solicitud_apertura SET justificacion = justificacion WHERE id = @id";
                comando.Parameters.AddWithValue("@id", solicitudId);
                await comando.ExecuteNonQueryAsync(cancellationToken);
            }

            await contexto.SaveChangesAsync(cancellationToken);
        }
    }
}
