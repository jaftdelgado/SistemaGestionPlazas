using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sgpla.BuildingBlocks.Application;

namespace Sgpla.BuildingBlocks.Infrastructure.Archivos;

public static class AlmacenamientoExtensions
{
    public static IServiceCollection AddAlmacenamientoArchivos(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<AlmacenamientoOptions>()
            .Bind(configuration.GetSection(AlmacenamientoOptions.Seccion))
            .ValidateDataAnnotations()
            .Validate(opciones => Path.IsPathRooted(opciones.RutaBase), "Almacenamiento:RutaBase debe ser una ruta absoluta.")
            .ValidateOnStart();
        services.AddSingleton<IAlmacenamientoArchivos, AlmacenamientoLocal>();

        return services;
    }
}
