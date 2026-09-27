using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.Institucional.Application.Ubicaciones;

namespace Sgpla.Modules.Institucional.Endpoints.Ubicaciones;

/// <summary>Regiones de solo lectura: los valores los carga la semilla (Modulo_Institucional.md §6, D1).</summary>
internal static class RegionEndpoints
{
    public static RouteGroupBuilder MapRegionEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/regiones").WithTags("Regiones");

        grupo.MapGet("/", Listar).WithName("ListarRegiones")
            .WithSummary("Lista todas las regiones, en orden de clave.");
        grupo.MapGet("/{id:int}", Obtener).WithName("ObtenerRegion")
            .WithSummary("Obtiene una región.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return modulo;
    }

    private static async Task<Results<Ok<IReadOnlyList<RegionResponse>>, ProblemHttpResult>> Listar(
        IQueryHandler<ListarRegionesQuery, IReadOnlyList<RegionResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListarRegionesQuery(), cancellationToken)).ToOk();

    private static async Task<Results<Ok<RegionResponse>, ProblemHttpResult>> Obtener(
        int id,
        IQueryHandler<ObtenerRegionQuery, RegionResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ObtenerRegionQuery(id), cancellationToken)).ToOk();
}
