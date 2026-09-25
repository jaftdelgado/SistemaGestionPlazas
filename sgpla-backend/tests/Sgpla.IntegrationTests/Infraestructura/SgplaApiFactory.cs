using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace Sgpla.IntegrationTests.Infraestructura;

/// <summary>Host de la API en memoria apuntando a la base de datos del contenedor.</summary>
public sealed class SgplaApiFactory(SqlServerFixture sqlServer) : WebApplicationFactory<Program>
{
    /// <summary>Logs emitidos por el host, con sus scopes.</summary>
    public ProveedorLogsEnMemoria Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Sgpla", sqlServer.CadenaConexion);

        // La regla es solo para este proveedor: captura desde Information sin cambiar lo que muestra la consola.
        builder.ConfigureLogging(logging => logging
            .AddProvider(Logs)
            .AddFilter<ProveedorLogsEnMemoria>(category: null, LogLevel.Information));
    }
}
