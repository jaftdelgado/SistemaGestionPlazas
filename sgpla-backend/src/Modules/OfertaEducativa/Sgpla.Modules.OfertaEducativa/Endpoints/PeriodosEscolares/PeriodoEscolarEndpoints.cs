using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.OfertaEducativa.Application.PeriodosEscolares;

namespace Sgpla.Modules.OfertaEducativa.Endpoints.PeriodosEscolares;

internal static class PeriodoEscolarEndpoints
{
    private const string NombreRutaObtener = "ObtenerPeriodoEscolar";

    public static RouteGroupBuilder MapPeriodoEscolarEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/periodos-escolares").WithTags("Periodos escolares");

        grupo.MapGet("/", Listar).WithName("ListarPeriodosEscolares")
            .WithSummary("Lista los periodos escolares activos, del más reciente al más antiguo.");
        grupo.MapGet("/{id:int}", Obtener).WithName(NombreRutaObtener)
            .WithSummary("Obtiene un periodo escolar activo.")
            .ProducesProblem(StatusCodes.Status404NotFound);
        grupo.MapPost("/", Crear).WithName("CrearPeriodoEscolar")
            .WithSummary("Registra un periodo escolar.")
            .RequireAuthorization(Politicas.Superusuario)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
        grupo.MapPut("/{id:int}", Modificar).WithName("ModificarPeriodoEscolar")
            .WithSummary("Modifica las fechas de un periodo escolar; la clave no cambia.")
            .RequireAuthorization(Politicas.Superusuario)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
        grupo.MapDelete("/{id:int}", DarDeBaja).WithName("DarDeBajaPeriodoEscolar")
            .WithSummary("Da de baja un periodo escolar sin programaciones ni otras referencias.")
            .RequireAuthorization(Politicas.Superusuario)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return modulo;
    }

    private static async Task<Results<Ok<IReadOnlyList<PeriodoEscolarResponse>>, ProblemHttpResult>> Listar(
        IQueryHandler<ListarPeriodosEscolaresQuery, IReadOnlyList<PeriodoEscolarResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListarPeriodosEscolaresQuery(), cancellationToken)).ToOk();

    private static async Task<Results<Ok<PeriodoEscolarResponse>, ProblemHttpResult>> Obtener(
        int id,
        IQueryHandler<ObtenerPeriodoEscolarQuery, PeriodoEscolarResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ObtenerPeriodoEscolarQuery(id), cancellationToken)).ToOk();

    /// <summary>El cuerpo coincide con el comando, así que se enlaza directamente sin un request propio.</summary>
    private static async Task<Results<CreatedAtRoute<PeriodoEscolarResponse>, ProblemHttpResult>> Crear(
        CrearPeriodoEscolarCommand command,
        ICommandHandler<CrearPeriodoEscolarCommand, PeriodoEscolarResponse> handler,
        CancellationToken cancellationToken)
    {
        var resultado = await handler.HandleAsync(command, cancellationToken);

        return resultado.IsSuccess
            ? TypedResults.CreatedAtRoute(resultado.Value, NombreRutaObtener, new { id = resultado.Value.Id })
            : resultado.Error.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Modificar(
        int id,
        ModificarPeriodoEscolarRequest request,
        ICommandHandler<ModificarPeriodoEscolarCommand> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(
            new ModificarPeriodoEscolarCommand(id, request.FechaInicio, request.FechaFin), cancellationToken))
            .ToNoContent();

    private static async Task<Results<NoContent, ProblemHttpResult>> DarDeBaja(
        int id,
        ICommandHandler<DarDeBajaPeriodoEscolarCommand> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new DarDeBajaPeriodoEscolarCommand(id), cancellationToken)).ToNoContent();
}

internal sealed record ModificarPeriodoEscolarRequest(DateOnly FechaInicio, DateOnly FechaFin);
