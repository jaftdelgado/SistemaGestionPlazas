using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.Institucional.Application.Ubicaciones;

namespace Sgpla.Modules.Institucional.Endpoints.Ubicaciones;

/// <summary>Campus de solo lectura: los valores los carga la semilla.</summary>
internal static class CampusEndpoints
{
    public static RouteGroupBuilder MapCampusEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/campus").WithTags("Campus");

        grupo.MapGet("/", Listar).WithName("ListarCampus")
            .WithSummary("Lista los campus, opcionalmente de una sola región.")
            .ProducesValidationProblem();
        grupo.MapGet("/{id:int}", Obtener).WithName("ObtenerCampus")
            .WithSummary("Obtiene un campus.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return modulo;
    }

    private static async Task<Results<Ok<IReadOnlyList<CampusResponse>>, ProblemHttpResult>> Listar(
        [AsParameters] ListarCampusRequest request,
        IQueryHandler<ListarCampusQuery, IReadOnlyList<CampusResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListarCampusQuery(request.RegionId), cancellationToken)).ToOk();

    private static async Task<Results<Ok<CampusResponse>, ProblemHttpResult>> Obtener(
        int id,
        IQueryHandler<ObtenerCampusQuery, CampusResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ObtenerCampusQuery(id), cancellationToken)).ToOk();
}

/// <summary>Parámetros de consulta del listado: <c>?regionId=2</c>.</summary>
internal sealed record ListarCampusRequest([FromQuery(Name = "regionId")] int? RegionId);
