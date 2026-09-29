using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.Catalogos.Application.CatalogosFijos;

namespace Sgpla.Modules.Catalogos.Endpoints.CatalogosFijos;

/// <summary>Catálogo fijo con clave (<c>clave</c>): solo lectura.</summary>
internal static class NivelFormacionEndpoints
{
    public static RouteGroupBuilder MapNivelFormacionEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/niveles-formacion").WithTags("Niveles de formación");

        grupo.MapGet("/", Listar).WithName("ListarNivelesFormacion")
            .WithSummary("Lista los niveles de formación, en orden de id.");
        grupo.MapGet("/{id:int}", Obtener).WithName("ObtenerNivelFormacion")
            .WithSummary("Obtiene un nivel de formación.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return modulo;
    }

    private static async Task<Results<Ok<IReadOnlyList<ClasificacionConClaveResponse>>, ProblemHttpResult>> Listar(
        IQueryHandler<ListarNivelesFormacionQuery, IReadOnlyList<ClasificacionConClaveResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListarNivelesFormacionQuery(), cancellationToken)).ToOk();

    private static async Task<Results<Ok<ClasificacionConClaveResponse>, ProblemHttpResult>> Obtener(
        int id,
        IQueryHandler<ObtenerNivelFormacionQuery, ClasificacionConClaveResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ObtenerNivelFormacionQuery(id), cancellationToken)).ToOk();
}
