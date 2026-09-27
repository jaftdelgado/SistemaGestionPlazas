using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.Catalogos.Application.CatalogosFijos;

namespace Sgpla.Modules.Catalogos.Endpoints.CatalogosFijos;

/// <summary>Catálogo fijo con una columna adicional (<c>requiereLugar</c>): solo lectura.</summary>
internal static class ModalidadRecepcionEndpoints
{
    public static RouteGroupBuilder MapModalidadRecepcionEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/modalidades-recepcion").WithTags("Modalidades de recepción");

        grupo.MapGet("/", Listar).WithName("ListarModalidadRecepcion")
            .WithSummary("Lista todas las modalidades de recepción, en orden de id.");
        grupo.MapGet("/{id:int}", Obtener).WithName("ObtenerModalidadRecepcion")
            .WithSummary("Obtiene una modalidad de recepción.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return modulo;
    }

    private static async Task<Results<Ok<IReadOnlyList<ModalidadRecepcionResponse>>, ProblemHttpResult>> Listar(
        IQueryHandler<ListarModalidadesRecepcionQuery, IReadOnlyList<ModalidadRecepcionResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListarModalidadesRecepcionQuery(), cancellationToken)).ToOk();

    private static async Task<Results<Ok<ModalidadRecepcionResponse>, ProblemHttpResult>> Obtener(
        int id,
        IQueryHandler<ObtenerModalidadRecepcionQuery, ModalidadRecepcionResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ObtenerModalidadRecepcionQuery(id), cancellationToken)).ToOk();
}
