using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Sgpla.IntegrationTests.Infraestructura;

/// <summary>
/// Proveedor de logs en memoria para las pruebas. Guarda cada entrada con los valores de sus
/// scopes (TraceId, SpanId, RequestPath...), que recibe del <see cref="IExternalScopeProvider"/> del host.
/// </summary>
public sealed class ProveedorLogsEnMemoria : ILoggerProvider, ISupportExternalScope
{
    private readonly ConcurrentQueue<EntradaLog> _entradas = new();
    private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();

    public IReadOnlyCollection<EntradaLog> Entradas => _entradas;

    public ILogger CreateLogger(string categoryName) => new LoggerEnMemoria(categoryName, this);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

    public void Dispose()
    {
    }

    private sealed class LoggerEnMemoria(string categoria, ProveedorLogsEnMemoria proveedor) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => proveedor._scopes.Push(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var scopes = new Dictionary<string, string?>();
            proveedor._scopes.ForEachScope(
                (scope, acumulado) =>
                {
                    if (scope is IEnumerable<KeyValuePair<string, object?>> pares)
                    {
                        foreach (var (clave, valor) in pares)
                        {
                            acumulado[clave] = valor?.ToString();
                        }
                    }
                },
                scopes);

            proveedor._entradas.Enqueue(new EntradaLog(categoria, logLevel, formatter(state, exception), exception?.ToString(), scopes));
        }
    }
}

/// <param name="Excepcion">La excepción adjunta, completa: es lo que un sink escribe junto al mensaje.</param>
public sealed record EntradaLog(
    string Categoria,
    LogLevel Nivel,
    string Mensaje,
    string? Excepcion,
    IReadOnlyDictionary<string, string?> Scopes)
{
    /// <summary>Si el texto aparece en el mensaje o en la excepción adjunta, sin distinguir mayúsculas.</summary>
    public bool Contiene(string texto) =>
        Mensaje.Contains(texto, StringComparison.OrdinalIgnoreCase)
        || (Excepcion?.Contains(texto, StringComparison.OrdinalIgnoreCase) ?? false);
}
