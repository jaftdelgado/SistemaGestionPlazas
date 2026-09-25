using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sgpla.BuildingBlocks.Infrastructure.Handlers;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Catalogos.Application.GradosAcademicos;
using Sgpla.Modules.Catalogos.Endpoints.GradosAcademicos;
using Sgpla.Modules.Catalogos.Infrastructure.GradosAcademicos;

namespace Sgpla.Modules.Catalogos;

/// <summary>Punto de registro del módulo Catalogos en el host.</summary>
public static class CatalogosModule
{
    public const string Ruta = "/api/v1/catalogos";

    public static IServiceCollection AddCatalogosModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var ensamblado = typeof(CatalogosModule).Assembly;

        services.AddPersistenciaModulo(ensamblado);
        services.AddHandlersModulo(ensamblado);

        services.AddScoped<IGradoAcademicoQueries, GradoAcademicoQueries>();

        return services;
    }

    public static IEndpointRouteBuilder MapCatalogosEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup(Ruta);
        // Autorización: cuando exista JWT (módulo Usuarios), los catálogos fijos quedan para cualquier usuario autenticado.

        grupo.MapGradoAcademicoEndpoints();

        return endpoints;
    }
}
