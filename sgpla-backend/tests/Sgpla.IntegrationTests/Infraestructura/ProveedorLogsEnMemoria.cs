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

            proveedor._entradas.Enqueue(new EntradaLog(categoria, logLevel, formatter(state, exception), scopes));
        }
    }
}

public sealed record EntradaLog(string Categoria, LogLevel Nivel, string Mensaje, IReadOnlyDictionary<string, string?> Scopes);
