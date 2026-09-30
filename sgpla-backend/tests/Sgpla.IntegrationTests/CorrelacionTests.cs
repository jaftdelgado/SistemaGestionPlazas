using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests;

/// <summary>
/// El traceId de una respuesta de error permite encontrar los logs de esa misma petición.
/// El error lo produce <c>ToProblem</c>, el helper que usan todos los endpoints, así que la prueba cubre a todos los módulos.
/// </summary>
public sealed partial class CorrelacionTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private readonly SgplaApiFactory _api = new(sqlServer);

    [Fact]
    public async Task RespuestaDeError_ConRecursoInexistente_TraceIdApareceEnLosLogsDeLaPeticion()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(
            new Uri($"/api/v1/catalogos/grados-academicos/{int.MaxValue}", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problema.GetProperty("codigo").GetString().ShouldBe("GradoAcademico.NoEncontrado");

        // La respuesta usa el formato W3C (00-<TraceId>-<SpanId>-<flags>); los logs, solo el TraceId.
        var traceIdW3C = problema.GetProperty("traceId").GetString() ?? string.Empty;
        var formato = FormatoW3C().Match(traceIdW3C);
        formato.Success.ShouldBeTrue($"El traceId '{traceIdW3C}' no tiene formato W3C.");
        var traceId = formato.Groups["traceId"].Value;

        _api.Logs.Entradas.ShouldContain(
            entrada => entrada.Scopes.GetValueOrDefault("TraceId") == traceId,
            $"Ningún log capturado lleva TraceId {traceId} en sus scopes.");
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    [GeneratedRegex("^00-(?<traceId>[0-9a-f]{32})-[0-9a-f]{16}-[0-9a-f]{2}$")]
    private static partial Regex FormatoW3C();
}
