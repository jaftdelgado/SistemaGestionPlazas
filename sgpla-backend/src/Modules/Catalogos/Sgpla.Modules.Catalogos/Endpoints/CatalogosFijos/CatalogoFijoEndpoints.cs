using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.Catalogos.Application.CatalogosFijos;

namespace Sgpla.Modules.Catalogos.Endpoints.CatalogosFijos;

/// <summary>
/// Rutas de solo lectura de un catálogo fijo que solo tiene nombre:
/// <c>GET /</c> con todos los valores en orden de id y <c>GET /{id}</c>. No hay rutas de escritura, así que un
/// <c>POST</c>, <c>PUT</c> o <c>DELETE</c> responde 405.
/// </summary>
internal static class CatalogoFijoEndpoints
{
    /// <param name="modulo">Grupo del módulo.</param>
    /// <param name="ruta">Recurso en plural y kebab-case: <c>/tipos-plaza</c>.</param>
    /// <param name="etiqueta">Nombre del recurso para OpenAPI: <c>Tipos de plaza</c>.</param>
    public static RouteGroupBuilder MapCatalogoFijo<TCatalogo>(this RouteGroupBuilder modulo, string ruta, string etiqueta)
    {
        var catalogo = typeof(TCatalogo).Name;
        var grupo = modulo.MapGroup(ruta).WithTags(etiqueta);

        grupo.MapGet("/", Listar<TCatalogo>).WithName($"Listar{catalogo}")
            .WithSummary("Lista todos los valores del catálogo, en orden de id.");
        grupo.MapGet("/{id:int}", Obtener<TCatalogo>).WithName($"Obtener{catalogo}")
            .WithSummary("Obtiene un valor del catálogo.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return modulo;
    }

    private static async Task<Results<Ok<IReadOnlyList<CatalogoFijoResponse>>, ProblemHttpResult>> Listar<TCatalogo>(
        IQueryHandler<ListarCatalogoFijoQuery<TCatalogo>, IReadOnlyList<CatalogoFijoResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListarCatalogoFijoQuery<TCatalogo>(), cancellationToken)).ToOk();

    private static async Task<Results<Ok<CatalogoFijoResponse>, ProblemHttpResult>> Obtener<TCatalogo>(
        int id,
        IQueryHandler<ObtenerCatalogoFijoQuery<TCatalogo>, CatalogoFijoResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ObtenerCatalogoFijoQuery<TCatalogo>(id), cancellationToken)).ToOk();
}
