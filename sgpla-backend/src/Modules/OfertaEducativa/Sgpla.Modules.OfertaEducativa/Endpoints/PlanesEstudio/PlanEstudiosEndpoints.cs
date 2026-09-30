using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.OfertaEducativa.Application.ExperienciasEducativas;
using Sgpla.Modules.OfertaEducativa.Application.PlanesEstudio;

namespace Sgpla.Modules.OfertaEducativa.Endpoints.PlanesEstudio;

internal static class PlanEstudiosEndpoints
{
    private const string NombreRutaObtener = "ObtenerPlanEstudios";

    public static RouteGroupBuilder MapPlanEstudiosEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/planes-estudio").WithTags("Planes de estudio");

        grupo.MapGet("/", Listar).WithName("ListarPlanesEstudio")
            .WithSummary("Lista los planes de estudio activos de tu ámbito, paginados y con filtros opcionales.")
            .ProducesValidationProblem();
        grupo.MapGet("/{id:int}", Obtener).WithName(NombreRutaObtener)
            .WithSummary("Obtiene un plan de estudios activo de tu ámbito.")
            .ProducesProblem(StatusCodes.Status404NotFound);
        grupo.MapGet("/{id:int}/experiencias-educativas", ListarExperienciasEducativas)
            .WithName("ListarExperienciasEducativasDePlan")
            .WithSummary("Lista las experiencias educativas activas del plan, en orden de materia y curso.")
            .ProducesProblem(StatusCodes.Status404NotFound);
        grupo.MapGet("/{id:int}/excel", Exportar).WithName("ExportarPlanEstudios")
            .WithSummary("Genera el Excel del plan con el formato de la UV.")
            .Produces(StatusCodes.Status200OK, contentType: ArchivoGenerado.TipoContenidoExcel)
            .ProducesProblem(StatusCodes.Status404NotFound);
        grupo.MapPost("/", Importar).WithName("ImportarPlanEstudios")
            .WithSummary("Registra un plan de estudios con sus experiencias educativas.")
            .RequireAuthorization(Politicas.Dgaa)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
        grupo.MapDelete("/{id:int}", DarDeBaja).WithName("DarDeBajaPlanEstudios")
            .WithSummary("Da de baja un plan de estudios y sus experiencias educativas.")
            .RequireAuthorization(Politicas.Dgaa)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return modulo;
    }

    private static async Task<Results<Ok<Pagina<PlanEstudiosResponse>>, ProblemHttpResult>> Listar(
        [AsParameters] ListarPlanesEstudioRequest request,
        IQueryHandler<ListarPlanesEstudioQuery, Pagina<PlanEstudiosResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(
            new ListarPlanesEstudioQuery(request.ComoPaginacion(), request.ComoFiltros()), cancellationToken))
            .ToOk();

    private static async Task<Results<Ok<PlanEstudiosResponse>, ProblemHttpResult>> Obtener(
        int id,
        IQueryHandler<ObtenerPlanEstudiosQuery, PlanEstudiosResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ObtenerPlanEstudiosQuery(id), cancellationToken)).ToOk();

    private static async Task<Results<Ok<IReadOnlyList<ExperienciaEducativaResponse>>, ProblemHttpResult>> ListarExperienciasEducativas(
        int id,
        IQueryHandler<ListarExperienciasEducativasDePlanQuery, IReadOnlyList<ExperienciaEducativaResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListarExperienciasEducativasDePlanQuery(id), cancellationToken)).ToOk();

    private static async Task<Results<FileContentHttpResult, ProblemHttpResult>> Exportar(
        int id,
        IQueryHandler<ExportarPlanEstudiosQuery, ArchivoGenerado> handler,
        CancellationToken cancellationToken)
    {
        var resultado = await handler.HandleAsync(new ExportarPlanEstudiosQuery(id), cancellationToken);

        return resultado.IsSuccess
            ? TypedResults.File(resultado.Value.Contenido, resultado.Value.TipoContenido, resultado.Value.Nombre)
            : resultado.Error.ToProblem();
    }

    /// <summary>Compone como el de programas: invoca el comando y después la consulta de obtener.</summary>
    private static async Task<Results<CreatedAtRoute<PlanEstudiosResponse>, ProblemHttpResult>> Importar(
        ImportarPlanEstudiosRequest request,
        ICommandHandler<ImportarPlanEstudiosCommand, int> comandoHandler,
        IQueryHandler<ObtenerPlanEstudiosQuery, PlanEstudiosResponse> consultaHandler,
        CancellationToken cancellationToken)
    {
        var creado = await comandoHandler.HandleAsync(request.ComoComando(), cancellationToken);
        if (creado.IsFailure)
        {
            return creado.Error.ToProblem();
        }

        var respuesta = await consultaHandler.HandleAsync(new ObtenerPlanEstudiosQuery(creado.Value), cancellationToken);
        return TypedResults.CreatedAtRoute(respuesta.Value, NombreRutaObtener, new { id = creado.Value });
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DarDeBaja(
        int id,
        ICommandHandler<DarDeBajaPlanEstudiosCommand> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new DarDeBajaPlanEstudiosCommand(id), cancellationToken)).ToNoContent();
}

/// <summary>Parámetros de consulta del listado; cada uno llega en camelCase, como lo envía el frontend.</summary>
internal sealed record ListarPlanesEstudioRequest(
    [FromQuery(Name = "pagina")] int? Pagina,
    [FromQuery(Name = "tamanoPagina")] int? TamanoPagina,
    [FromQuery(Name = "programaEducativoId")] int? ProgramaEducativoId,
    [FromQuery(Name = "entidadAcademicaId")] int? EntidadAcademicaId,
    [FromQuery(Name = "busqueda")] string? Busqueda)
{
    public Paginacion ComoPaginacion() => new(Pagina ?? 1, TamanoPagina ?? Paginacion.TamanoPorOmision);

    public FiltrosPlanesEstudio ComoFiltros() => new(ProgramaEducativoId, EntidadAcademicaId, Busqueda);
}

/// <param name="ExperienciasEducativas">Si llega <c>null</c> o se omite, se toma como una lista vacía.</param>
internal sealed record ImportarPlanEstudiosRequest(
    int ProgramaEducativoId,
    string Codigo,
    IReadOnlyList<ExperienciaEducativaEntrada>? ExperienciasEducativas)
{
    public ImportarPlanEstudiosCommand ComoComando() => new(ProgramaEducativoId, Codigo, ExperienciasEducativas ?? []);
}
