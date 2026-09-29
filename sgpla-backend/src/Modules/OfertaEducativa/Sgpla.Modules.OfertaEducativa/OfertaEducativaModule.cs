using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Handlers;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Application.Ambito;
using Sgpla.Modules.OfertaEducativa.Application.PeriodosEscolares;
using Sgpla.Modules.OfertaEducativa.Application.ProgramasEducativos;
using Sgpla.Modules.OfertaEducativa.Endpoints.PeriodosEscolares;
using Sgpla.Modules.OfertaEducativa.Endpoints.ProgramasEducativos;
using Sgpla.Modules.OfertaEducativa.Infrastructure.Ambito;
using Sgpla.Modules.OfertaEducativa.Infrastructure.Contratos;
using Sgpla.Modules.OfertaEducativa.Infrastructure.PeriodosEscolares;
using Sgpla.Modules.OfertaEducativa.Infrastructure.ProgramasEducativos;

namespace Sgpla.Modules.OfertaEducativa;

/// <summary>Punto de registro del módulo OfertaEducativa en el host.</summary>
public static class OfertaEducativaModule
{
    public const string Ruta = "/api/v1/oferta-educativa";

    public static IServiceCollection AddOfertaEducativaModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var ensamblado = typeof(OfertaEducativaModule).Assembly;

        services.AddPersistenciaModulo(ensamblado);
        services.AddHandlersModulo(ensamblado);

        services.AddScoped<IAmbitoOfertaEducativa, AmbitoOfertaEducativa>();
        services.AddScoped<IPeriodoEscolarRepository, PeriodoEscolarRepository>();
        services.AddScoped<IProgramaEducativoRepository, ProgramaEducativoRepository>();

        // Contratos para otros módulos (Institucional). IReferenciasPeriodoEscolar lo registra cada módulo que lo implementa.
        services.AddScoped<IProgramasDeEntidadAcademica, ProgramasDeEntidadAcademica>();

        return services;
    }

    public static IEndpointRouteBuilder MapOfertaEducativaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup(Ruta).RequireAuthorization(Politicas.Autenticado);
        grupo.ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden);

        grupo.MapPeriodoEscolarEndpoints();
        grupo.MapProgramaEducativoEndpoints();

        return endpoints;
    }
}
