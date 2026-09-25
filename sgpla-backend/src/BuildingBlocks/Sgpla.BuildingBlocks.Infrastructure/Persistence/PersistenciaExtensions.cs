using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sgpla.BuildingBlocks.Application;

namespace Sgpla.BuildingBlocks.Infrastructure.Persistence;

/// <summary>Ensamblado de un módulo cuyas configuraciones de EF Core se aplican a <see cref="SgplaDbContext"/>.</summary>
public sealed record EnsambladoModulo(Assembly Ensamblado);

public static class PersistenciaExtensions
{
    public const string NombreCadenaConexion = "Sgpla";

    public static IServiceCollection AddPersistenciaSgpla(this IServiceCollection services, IConfiguration configuration)
    {
        var cadenaConexion = configuration.GetConnectionString(NombreCadenaConexion);
        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException($"Falta la cadena de conexión '{NombreCadenaConexion}'.");
        }

        services.AddDbContext<SgplaDbContext>(options => options
            .UseSqlServer(cadenaConexion)
            .UseSnakeCaseNamingConvention()
            // SaveChangesFailed incluye en su mensaje la excepción completa, y la de una violación de unicidad
            // (2601/2627) trae el valor duplicado (ESTANDAR_MODULOS.md, sección 11). La excepción no se pierde:
            // la traduce el manejador global de unicidad o la registra UseExceptionHandler.
            .ConfigureWarnings(advertencias => advertencias.Ignore(CoreEventId.SaveChangesFailed)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>Registra el ensamblado de un módulo para que <see cref="SgplaDbContext"/> aplique sus configuraciones.</summary>
    public static IServiceCollection AddPersistenciaModulo(this IServiceCollection services, Assembly ensamblado)
    {
        services.AddSingleton(new EnsambladoModulo(ensamblado));
        return services;
    }
}
