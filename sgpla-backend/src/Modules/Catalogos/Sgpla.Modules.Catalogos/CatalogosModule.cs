using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Handlers;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Catalogos.Application.Articulos;
using Sgpla.Modules.Catalogos.Application.CatalogosFijos;
using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.Catalogos.Domain.CatalogosFijos;
using Sgpla.Modules.Catalogos.Endpoints.Articulos;
using Sgpla.Modules.Catalogos.Endpoints.CatalogosFijos;
using Sgpla.Modules.Catalogos.Infrastructure.Articulos;
using Sgpla.Modules.Catalogos.Infrastructure.CatalogosFijos;

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

        // Catálogos fijos que solo tienen nombre: handlers genéricos. Los demás se registran por escaneo,
        // incluidos Municipio, TratamientoAcademico y ModalidadRecepcion, que tienen consultas propias.
        services.AddCatalogoFijo<GradoAcademico>();
        services.AddCatalogoFijo<TipoDocumentoExpediente>();
        services.AddCatalogoFijo<TipoPlaza>();
        services.AddCatalogoFijo<TipoContratacion>();

        // Artículos. IReferenciasArticulo lo registra cada módulo que lo implementa (Publicacion).
        services.AddScoped<IArticuloRepository, ArticuloRepository>();

        // Contratos para otros módulos (Institucional).
        services.AddScoped<IMunicipios, Municipios>();

        return services;
    }

    public static IEndpointRouteBuilder MapCatalogosEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup(Ruta).RequireAuthorization(Politicas.Autenticado);
        grupo.ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden);

        grupo.MapCatalogoFijo<GradoAcademico>("/grados-academicos", "Grados académicos");
        grupo.MapCatalogoFijo<TipoDocumentoExpediente>("/tipos-documento-expediente", "Tipos de documento de expediente");
        grupo.MapCatalogoFijo<TipoPlaza>("/tipos-plaza", "Tipos de plaza");
        grupo.MapCatalogoFijo<TipoContratacion>("/tipos-contratacion", "Tipos de contratación");
        grupo.MapMunicipioEndpoints();
        grupo.MapTratamientoAcademicoEndpoints();
        grupo.MapModalidadRecepcionEndpoints();
        grupo.MapArticuloEndpoints();

        return endpoints;
    }

    private static void AddCatalogoFijo<TCatalogo>(this IServiceCollection services)
        where TCatalogo : CatalogoFijo
    {
        services.AddScoped<
            IQueryHandler<ListarCatalogoFijoQuery<TCatalogo>, IReadOnlyList<CatalogoFijoResponse>>,
            ListarCatalogoFijoHandler<TCatalogo>>();
        services.AddScoped<
            IQueryHandler<ObtenerCatalogoFijoQuery<TCatalogo>, CatalogoFijoResponse>,
            ObtenerCatalogoFijoHandler<TCatalogo>>();
    }
}
