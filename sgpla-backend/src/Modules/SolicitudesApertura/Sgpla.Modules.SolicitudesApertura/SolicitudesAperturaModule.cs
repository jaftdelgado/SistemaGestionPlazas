using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Handlers;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.SolicitudesApertura.Application.Periodos;
using Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;
using Sgpla.Modules.SolicitudesApertura.Endpoints.Periodos;
using Sgpla.Modules.SolicitudesApertura.Endpoints.SolicitudesApertura;
using Sgpla.Modules.SolicitudesApertura.Infrastructure.Contratos;
using Sgpla.Modules.SolicitudesApertura.Infrastructure.Periodos;
using Sgpla.Modules.SolicitudesApertura.Infrastructure.SolicitudesApertura;

namespace Sgpla.Modules.SolicitudesApertura;

/// <summary>Punto de registro del módulo SolicitudesApertura en el host.</summary>
public static class SolicitudesAperturaModule
{
    public const string Ruta = "/api/v1/solicitudes-apertura";

    public static IServiceCollection AddSolicitudesAperturaModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var ensamblado = typeof(SolicitudesAperturaModule).Assembly;

        services.AddPersistenciaModulo(ensamblado);
        services.AddHandlersModulo(ensamblado);
        services.AddOptions<PeriodosOptions>()
            .Bind(configuration.GetSection(PeriodosOptions.Seccion))
            .ValidateDataAnnotations()
            .Validate(
                opciones => opciones.PeriodoActual != opciones.PeriodoSiguiente,
                "SolicitudesApertura:PeriodoActual y PeriodoSiguiente deben ser distintos.")
            .ValidateOnStart();
        services.AddSingleton<IPeriodosConfigurados, PeriodosConfigurados>();
        services.AddScoped<ISolicitudAperturaRepository, SolicitudAperturaRepository>();

        // Contratos de OfertaEducativa que este módulo implementa (bajas de EE, plan y periodo).
        services.AddScoped<IReferenciasExperienciaEducativa, ReferenciasExperienciaEducativaEnSolicitudes>();
        services.AddScoped<IReferenciasPeriodoEscolar, ReferenciasPeriodoEscolarEnSolicitudes>();

        return services;
    }

    public static IEndpointRouteBuilder MapSolicitudesAperturaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup(Ruta).RequireAuthorization(Politicas.Autenticado);
        grupo.ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden);
        grupo.MapSolicitudAperturaEndpoints();
        grupo.MapPeriodoEndpoints();
        return endpoints;
    }
}
