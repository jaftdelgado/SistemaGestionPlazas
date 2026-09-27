using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sgpla.BuildingBlocks.Infrastructure.Handlers;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Institucional.Endpoints.Ubicaciones;

namespace Sgpla.Modules.Institucional;

/// <summary>Punto de registro del módulo Institucional en el host.</summary>
public static class InstitucionalModule
{
    public const string Ruta = "/api/v1/institucional";

    public static IServiceCollection AddInstitucionalModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var ensamblado = typeof(InstitucionalModule).Assembly;

        services.AddPersistenciaModulo(ensamblado);
        services.AddHandlersModulo(ensamblado);

        return services;
    }

    public static IEndpointRouteBuilder MapInstitucionalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup(Ruta);
        // Autorización: cuando exista JWT (módulo Usuarios), regiones, campus y áreas quedan para cualquier usuario
        // autenticado; la lectura de entidades se filtra por ámbito (DGAA: las de su área; Entidad Académica: la
        // suya) y la escritura es solo del Superusuario (Modulo_Institucional.md, decisión D7).

        grupo.MapRegionEndpoints();
        grupo.MapCampusEndpoints();

        return endpoints;
    }
}
