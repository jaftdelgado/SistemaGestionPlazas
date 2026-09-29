using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.Catalogos.Application.CatalogosFijos;

namespace Sgpla.Modules.Catalogos.Endpoints.CatalogosFijos;

/// <summary>Catálogo fijo con clave (<c>clave</c>): solo lectura.</summary>
internal static class AreaFormacionEndpoints
{
    public static RouteGroupBuilder MapAreaFormacionEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/areas-formacion").WithTags("Áreas de formación");

        grupo.MapGet("/", Listar).WithName("ListarAreasFormacion")
            .WithSummary("Lista las áreas de formación, en orden de id.");
        grupo.MapGet("/{id:int}", Obtener).WithName("ObtenerAreaFormacion")
            .WithSummary("Obtiene un área de formación.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return modulo;
    }

    private static async Task<Results<Ok<IReadOnlyList<ClasificacionConClaveResponse>>, ProblemHttpResult>> Listar(
        IQueryHandler<ListarAreasFormacionQuery, IReadOnlyList<ClasificacionConClaveResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListarAreasFormacionQuery(), cancellationToken)).ToOk();

    private static async Task<Results<Ok<ClasificacionConClaveResponse>, ProblemHttpResult>> Obtener(
        int id,
        IQueryHandler<ObtenerAreaFormacionQuery, ClasificacionConClaveResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ObtenerAreaFormacionQuery(id), cancellationToken)).ToOk();
}
