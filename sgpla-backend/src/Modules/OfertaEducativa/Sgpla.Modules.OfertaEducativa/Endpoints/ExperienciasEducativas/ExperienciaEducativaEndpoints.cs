using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.OfertaEducativa.Application.ExperienciasEducativas;

namespace Sgpla.Modules.OfertaEducativa.Endpoints.ExperienciasEducativas;

internal static class ExperienciaEducativaEndpoints
{
    private const string NombreRutaObtener = "ObtenerExperienciaEducativa";

    /// <remarks>No hay un listado global: las EE de un plan están en <c>GET /planes-estudio/{id}/experiencias-educativas</c>.</remarks>
    public static RouteGroupBuilder MapExperienciaEducativaEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/experiencias-educativas").WithTags("Experiencias educativas");

        grupo.MapGet("/{id:int}", Obtener).WithName(NombreRutaObtener)
            .WithSummary("Obtiene una experiencia educativa activa de tu ámbito.")
            .ProducesProblem(StatusCodes.Status404NotFound);
        grupo.MapPost("/", Crear).WithName("CrearExperienciaEducativa")
            .WithSummary("Agrega una experiencia educativa a un plan de estudios.")
            .RequireAuthorization(Politicas.Dgaa)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
        grupo.MapPut("/{id:int}", Modificar).WithName("ModificarExperienciaEducativa")
            .WithSummary("Modifica una experiencia educativa; materia, curso y plan no cambian.")
            .RequireAuthorization(Politicas.Dgaa)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        grupo.MapDelete("/{id:int}", DarDeBaja).WithName("DarDeBajaExperienciaEducativa")
            .WithSummary("Da de baja una experiencia educativa sin programaciones activas.")
            .RequireAuthorization(Politicas.Dgaa)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return modulo;
    }

    private static async Task<Results<Ok<ExperienciaEducativaResponse>, ProblemHttpResult>> Obtener(
        int id,
        IQueryHandler<ObtenerExperienciaEducativaQuery, ExperienciaEducativaResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ObtenerExperienciaEducativaQuery(id), cancellationToken)).ToOk();

    /// <summary>Compone como el de programas: invoca el comando y después la consulta de obtener.</summary>
    private static async Task<Results<CreatedAtRoute<ExperienciaEducativaResponse>, ProblemHttpResult>> Crear(
        CrearExperienciaEducativaRequest request,
        ICommandHandler<CrearExperienciaEducativaCommand, int> comandoHandler,
        IQueryHandler<ObtenerExperienciaEducativaQuery, ExperienciaEducativaResponse> consultaHandler,
        CancellationToken cancellationToken)
    {
        var creado = await comandoHandler.HandleAsync(request.ComoComando(), cancellationToken);
        if (creado.IsFailure)
        {
            return creado.Error.ToProblem();
        }

        var respuesta = await consultaHandler.HandleAsync(new ObtenerExperienciaEducativaQuery(creado.Value), cancellationToken);
        return TypedResults.CreatedAtRoute(respuesta.Value, NombreRutaObtener, new { id = creado.Value });
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Modificar(
        int id,
        ModificarExperienciaEducativaRequest request,
        ICommandHandler<ModificarExperienciaEducativaCommand> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(request.ComoComando(id), cancellationToken)).ToNoContent();

    private static async Task<Results<NoContent, ProblemHttpResult>> DarDeBaja(
        int id,
        ICommandHandler<DarDeBajaExperienciaEducativaCommand> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new DarDeBajaExperienciaEducativaCommand(id), cancellationToken)).ToNoContent();
}

/// <summary><c>planEstudiosId</c> más los campos de la EE, en un solo nivel.</summary>
internal sealed record CrearExperienciaEducativaRequest(
    int PlanEstudiosId,
    string Nombre,
    string Materia,
    string Curso,
    int HorasTeoricas,
    int HorasPracticas,
    int Creditos,
    int? CupoMinimo,
    int? CupoMaximo,
    string? PerfilDocente,
    int AreaFormacionId)
{
    public CrearExperienciaEducativaCommand ComoComando() => new(
        PlanEstudiosId,
        new ExperienciaEducativaEntrada(
            Nombre,
            Materia,
            Curso,
            HorasTeoricas,
            HorasPracticas,
            Creditos,
            CupoMinimo,
            CupoMaximo,
            PerfilDocente,
            AreaFormacionId));
}

/// <summary>Los campos del comando sin el <c>id</c>, que viene de la ruta.</summary>
internal sealed record ModificarExperienciaEducativaRequest(
    string Nombre,
    int HorasTeoricas,
    int HorasPracticas,
    int Creditos,
    int? CupoMinimo,
    int? CupoMaximo,
    string? PerfilDocente,
    int AreaFormacionId)
{
    public ModificarExperienciaEducativaCommand ComoComando(int id) => new(
        id,
        Nombre,
        HorasTeoricas,
        HorasPracticas,
        Creditos,
        CupoMinimo,
        CupoMaximo,
        PerfilDocente,
        AreaFormacionId);
}
