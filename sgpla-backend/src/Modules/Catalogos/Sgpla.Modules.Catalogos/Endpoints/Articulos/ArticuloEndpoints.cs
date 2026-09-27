using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.Catalogos.Application.Articulos;

namespace Sgpla.Modules.Catalogos.Endpoints.Articulos;

internal static class ArticuloEndpoints
{
    private const string NombreRutaObtener = "ObtenerArticulo";

    public static RouteGroupBuilder MapArticuloEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/articulos").WithTags("Artículos");
        // Autorización: cuando exista JWT (módulo Usuarios), registrar y modificar quedan solo para el Superusuario.

        grupo.MapGet("/", Listar).WithName("ListarArticulos")
            .WithSummary("Lista todos los artículos, en orden alfabético del número.");
        grupo.MapGet("/{id:int}", Obtener).WithName(NombreRutaObtener).WithSummary("Obtiene un artículo.")
            .ProducesProblem(StatusCodes.Status404NotFound);
        grupo.MapPost("/", Crear).WithName("CrearArticulo").WithSummary("Registra un artículo.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
        grupo.MapPut("/{id:int}", Modificar).WithName("ModificarArticulo")
            .WithSummary("Modifica el artículo: la descripción siempre; el número, solo mientras ningún Aviso lo use.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return modulo;
    }

    private static async Task<Results<Ok<IReadOnlyList<ArticuloResponse>>, ProblemHttpResult>> Listar(
        IQueryHandler<ListarArticulosQuery, IReadOnlyList<ArticuloResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListarArticulosQuery(), cancellationToken)).ToOk();

    private static async Task<Results<Ok<ArticuloResponse>, ProblemHttpResult>> Obtener(
        int id,
        IQueryHandler<ObtenerArticuloQuery, ArticuloResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ObtenerArticuloQuery(id), cancellationToken)).ToOk();

    /// <summary>El cuerpo coincide con el comando, así que se enlaza directamente sin un request propio.</summary>
    private static async Task<Results<CreatedAtRoute<ArticuloResponse>, ProblemHttpResult>> Crear(
        CrearArticuloCommand command,
        ICommandHandler<CrearArticuloCommand, ArticuloResponse> handler,
        CancellationToken cancellationToken)
    {
        var resultado = await handler.HandleAsync(command, cancellationToken);

        return resultado.IsSuccess
            ? TypedResults.CreatedAtRoute(resultado.Value, NombreRutaObtener, new { id = resultado.Value.Id })
            : resultado.Error.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Modificar(
        int id,
        ModificarArticuloRequest request,
        ICommandHandler<ModificarArticuloCommand> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ModificarArticuloCommand(id, request.Numero, request.Descripcion), cancellationToken))
            .ToNoContent();
}

/// <param name="Descripcion">Opcional: si no llega (o llega <c>null</c>), se conserva la descripción actual.</param>
internal sealed record ModificarArticuloRequest(string Numero, string? Descripcion);
