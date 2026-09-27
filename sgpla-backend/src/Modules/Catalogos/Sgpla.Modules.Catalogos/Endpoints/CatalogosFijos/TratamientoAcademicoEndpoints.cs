using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.Catalogos.Application.CatalogosFijos;

namespace Sgpla.Modules.Catalogos.Endpoints.CatalogosFijos;

/// <summary>Catálogo fijo con grado académico y filtro por grado: solo lectura.</summary>
internal static class TratamientoAcademicoEndpoints
{
    public static RouteGroupBuilder MapTratamientoAcademicoEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/tratamientos-academicos").WithTags("Tratamientos académicos");

        grupo.MapGet("/", Listar).WithName("ListarTratamientoAcademico")
            .WithSummary("Lista los tratamientos académicos, opcionalmente de un solo grado.")
            .ProducesValidationProblem();
        grupo.MapGet("/{id:int}", Obtener).WithName("ObtenerTratamientoAcademico")
            .WithSummary("Obtiene un tratamiento académico.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return modulo;
    }

    private static async Task<Results<Ok<IReadOnlyList<TratamientoAcademicoResponse>>, ProblemHttpResult>> Listar(
        [AsParameters] ListarTratamientosAcademicosRequest request,
        IQueryHandler<ListarTratamientosAcademicosQuery, IReadOnlyList<TratamientoAcademicoResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListarTratamientosAcademicosQuery(request.GradoAcademicoId), cancellationToken))
            .ToOk();

    private static async Task<Results<Ok<TratamientoAcademicoResponse>, ProblemHttpResult>> Obtener(
        int id,
        IQueryHandler<ObtenerTratamientoAcademicoQuery, TratamientoAcademicoResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ObtenerTratamientoAcademicoQuery(id), cancellationToken)).ToOk();
}

/// <summary>Parámetros de consulta del listado: <c>?gradoAcademicoId=4</c>.</summary>
internal sealed record ListarTratamientosAcademicosRequest(
    [FromQuery(Name = "gradoAcademicoId")] int? GradoAcademicoId);
