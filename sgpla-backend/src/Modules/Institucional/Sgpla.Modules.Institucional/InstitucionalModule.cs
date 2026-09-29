using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Handlers;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Institucional.Application.AreasAcademicas;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.Institucional.Application.EntidadesAcademicas;
using Sgpla.Modules.Institucional.Endpoints.AreasAcademicas;
using Sgpla.Modules.Institucional.Endpoints.EntidadesAcademicas;
using Sgpla.Modules.Institucional.Endpoints.Ubicaciones;
using Sgpla.Modules.Institucional.Infrastructure.AreasAcademicas;
using Sgpla.Modules.Institucional.Infrastructure.Contratos;
using Sgpla.Modules.Institucional.Infrastructure.EntidadesAcademicas;

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

        services.AddScoped<IAreaAcademicaRepository, AreaAcademicaRepository>();
        services.AddScoped<IEntidadAcademicaRepository, EntidadAcademicaRepository>();
        services.AddScoped<IAmbitosInstitucionales, AmbitosInstitucionales>();

        return services;
    }

    public static IEndpointRouteBuilder MapInstitucionalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup(Ruta).RequireAuthorization(Politicas.Autenticado);
        grupo.ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden);

        grupo.MapRegionEndpoints();
        grupo.MapCampusEndpoints();
        grupo.MapAreaAcademicaEndpoints();
        grupo.MapEntidadAcademicaEndpoints();

        return endpoints;
    }
}
