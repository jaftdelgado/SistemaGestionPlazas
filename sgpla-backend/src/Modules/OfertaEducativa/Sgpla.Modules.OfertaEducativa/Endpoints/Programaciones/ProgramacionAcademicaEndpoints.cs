using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.OfertaEducativa.Application.Programaciones;

namespace Sgpla.Modules.OfertaEducativa.Endpoints.Programaciones;

internal static class ProgramacionAcademicaEndpoints
{
    public static RouteGroupBuilder MapProgramacionAcademicaEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/programaciones-academicas").WithTags("Programaciones académicas");

        grupo.MapGet("/", Listar).WithName("ListarProgramacionesAcademicas")
            .WithSummary("Lista las programaciones académicas activas de tu ámbito, paginadas y con filtros opcionales.")
            .ProducesValidationProblem();
        grupo.MapGet("/{id:int}", Obtener).WithName("ObtenerProgramacionAcademica")
            .WithSummary("Obtiene una programación académica activa de tu ámbito.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return modulo;
    }

    private static async Task<Results<Ok<Pagina<ProgramacionAcademicaResponse>>, ProblemHttpResult>> Listar(
        [AsParameters] ListarProgramacionesAcademicasRequest request,
        IQueryHandler<ListarProgramacionesAcademicasQuery, Pagina<ProgramacionAcademicaResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(
            new ListarProgramacionesAcademicasQuery(request.ComoPaginacion(), request.ComoFiltros()), cancellationToken))
            .ToOk();

    private static async Task<Results<Ok<ProgramacionAcademicaResponse>, ProblemHttpResult>> Obtener(
        int id,
        IQueryHandler<ObtenerProgramacionAcademicaQuery, ProgramacionAcademicaResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ObtenerProgramacionAcademicaQuery(id), cancellationToken)).ToOk();
}

/// <summary>Parámetros de consulta del listado; cada uno llega en camelCase, como lo envía el frontend.</summary>
internal sealed record ListarProgramacionesAcademicasRequest(
    [FromQuery(Name = "pagina")] int? Pagina,
    [FromQuery(Name = "tamanoPagina")] int? TamanoPagina,
    [FromQuery(Name = "periodoEscolarId")] int? PeriodoEscolarId,
    [FromQuery(Name = "entidadAcademicaId")] int? EntidadAcademicaId,
    [FromQuery(Name = "programaEducativoId")] int? ProgramaEducativoId,
    [FromQuery(Name = "planEstudiosId")] int? PlanEstudiosId,
    [FromQuery(Name = "experienciaEducativaId")] int? ExperienciaEducativaId,
    [FromQuery(Name = "nrc")] string? Nrc)
{
    public Paginacion ComoPaginacion() => new(Pagina ?? 1, TamanoPagina ?? Paginacion.TamanoPorOmision);

    public FiltrosProgramacionesAcademicas ComoFiltros() =>
        new(PeriodoEscolarId, EntidadAcademicaId, ProgramaEducativoId, PlanEstudiosId, ExperienciaEducativaId, Nrc);
}
