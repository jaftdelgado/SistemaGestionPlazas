using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.Institucional.Application.AreasAcademicas;

namespace Sgpla.Modules.Institucional.Endpoints.AreasAcademicas;

internal static class AreaAcademicaEndpoints
{
    private const string NombreRutaObtener = "ObtenerAreaAcademica";

    public static RouteGroupBuilder MapAreaAcademicaEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/areas-academicas").WithTags("Áreas académicas");
        // Autorización: cuando exista JWT (módulo Usuarios), regiones, campus y áreas quedan para cualquier usuario
        // autenticado, y la escritura es solo del Superusuario (Modulo_Institucional.md, decisión D7).

        grupo.MapGet("/", Listar).WithName("ListarAreasAcademicas")
            .WithSummary("Lista todas las áreas académicas activas, en orden de clave.");
        grupo.MapGet("/{id:int}", Obtener).WithName(NombreRutaObtener)
            .WithSummary("Obtiene un área académica activa.")
            .ProducesProblem(StatusCodes.Status404NotFound);
        grupo.MapPost("/", Crear).WithName("CrearAreaAcademica")
            .WithSummary("Registra un área académica.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
        grupo.MapPut("/{id:int}", Modificar).WithName("ModificarAreaAcademica")
            .WithSummary("Modifica el nombre y los datos de contacto de un área académica.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
        grupo.MapDelete("/{id:int}", DarDeBaja).WithName("DarDeBajaAreaAcademica")
            .WithSummary("Da de baja un área académica sin entidades académicas activas.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return modulo;
    }

    private static async Task<Results<Ok<IReadOnlyList<AreaAcademicaResponse>>, ProblemHttpResult>> Listar(
        IQueryHandler<ListarAreasAcademicasQuery, IReadOnlyList<AreaAcademicaResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListarAreasAcademicasQuery(), cancellationToken)).ToOk();

    private static async Task<Results<Ok<AreaAcademicaResponse>, ProblemHttpResult>> Obtener(
        int id,
        IQueryHandler<ObtenerAreaAcademicaQuery, AreaAcademicaResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ObtenerAreaAcademicaQuery(id), cancellationToken)).ToOk();

    /// <summary>El cuerpo coincide con el comando, así que se enlaza directamente sin un request propio.</summary>
    private static async Task<Results<CreatedAtRoute<AreaAcademicaResponse>, ProblemHttpResult>> Crear(
        CrearAreaAcademicaCommand command,
        ICommandHandler<CrearAreaAcademicaCommand, AreaAcademicaResponse> handler,
        CancellationToken cancellationToken)
    {
        var resultado = await handler.HandleAsync(command, cancellationToken);

        return resultado.IsSuccess
            ? TypedResults.CreatedAtRoute(resultado.Value, NombreRutaObtener, new { id = resultado.Value.Id })
            : resultado.Error.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Modificar(
        int id,
        ModificarAreaAcademicaRequest request,
        ICommandHandler<ModificarAreaAcademicaCommand> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(
            new ModificarAreaAcademicaCommand(id, request.Nombre, request.Telefono, request.Extension), cancellationToken))
            .ToNoContent();

    private static async Task<Results<NoContent, ProblemHttpResult>> DarDeBaja(
        int id,
        ICommandHandler<DarDeBajaAreaAcademicaCommand> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new DarDeBajaAreaAcademicaCommand(id), cancellationToken)).ToNoContent();
}

internal sealed record ModificarAreaAcademicaRequest(string Nombre, string Telefono, string? Extension);
