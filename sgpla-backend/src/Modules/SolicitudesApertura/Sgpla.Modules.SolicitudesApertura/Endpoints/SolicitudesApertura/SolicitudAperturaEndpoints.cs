using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;

namespace Sgpla.Modules.SolicitudesApertura.Endpoints.SolicitudesApertura;

internal static class SolicitudAperturaEndpoints
{
    private const string NombreRutaObtener = "ObtenerSolicitudApertura";

    public static RouteGroupBuilder MapSolicitudAperturaEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/solicitudes").WithTags("Solicitudes de apertura");

        grupo.MapGet("/", Listar).WithName("ListarSolicitudesApertura")
            .WithSummary("Lista las solicitudes de apertura del ámbito, de la más reciente a la más antigua.")
            .ProducesValidationProblem();
        grupo.MapGet("/{id:int}", Obtener).WithName(NombreRutaObtener)
            .WithSummary("Obtiene una solicitud de apertura del ámbito.")
            .ProducesProblem(StatusCodes.Status404NotFound);
        grupo.MapGet("/{id:int}/oficio", DescargarOficio).WithName("DescargarOficioSolicitudApertura")
            .WithSummary("Descarga el oficio de respaldo de una solicitud de apertura.")
            .RequireAuthorization(Politicas.DgaaOEntidadAcademica)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status404NotFound);
        grupo.MapPost("/", Crear).WithName("CrearSolicitudApertura")
            .WithSummary("Registra una solicitud de apertura para el periodo siguiente, con su oficio en PDF.")
            // La API se autentica con token bearer, sin cookies: no hay CSRF que prevenir.
            .DisableAntiforgery()
            .RequireAuthorization(Politicas.EntidadAcademica)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        return modulo;
    }

    private static async Task<Results<Ok<Pagina<SolicitudAperturaResponse>>, ProblemHttpResult>> Listar(
        [AsParameters] ListarSolicitudesAperturaRequest request,
        IQueryHandler<ListarSolicitudesAperturaQuery, Pagina<SolicitudAperturaResponse>> handler,
        CancellationToken cancellationToken)
    {
        var query = new ListarSolicitudesAperturaQuery(
            request.ComoPaginacion(),
            new FiltrosSolicitudesApertura(
                request.Estado,
                request.PeriodoEscolarId,
                request.ExperienciaEducativaId,
                request.EntidadAcademicaId));
        return (await handler.HandleAsync(query, cancellationToken)).ToOk();
    }

    private static async Task<Results<Ok<SolicitudAperturaResponse>, ProblemHttpResult>> Obtener(
        int id,
        IQueryHandler<ObtenerSolicitudAperturaQuery, SolicitudAperturaResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ObtenerSolicitudAperturaQuery(id), cancellationToken)).ToOk();

    private static async Task<Results<FileStreamHttpResult, ProblemHttpResult>> DescargarOficio(
        int id,
        IQueryHandler<DescargarOficioSolicitudAperturaQuery, OficioDescarga> handler,
        CancellationToken cancellationToken)
    {
        var resultado = await handler.HandleAsync(new DescargarOficioSolicitudAperturaQuery(id), cancellationToken);
        return resultado.IsSuccess
            ? TypedResults.File(resultado.Value.Contenido, "application/pdf", resultado.Value.Nombre)
            : resultado.Error.ToProblem();
    }

    private static async Task<Results<CreatedAtRoute<SolicitudAperturaResponse>, ProblemHttpResult>> Crear(
        [FromForm(Name = "experienciaEducativaId")] int? experienciaEducativaId,
        [FromForm(Name = "periodoEscolarId")] int? periodoEscolarId,
        [FromForm(Name = "seccion")] string? seccion,
        [FromForm(Name = "cantidadEstudiantes")] int? cantidadEstudiantes,
        [FromForm(Name = "justificacion")] string? justificacion,
        [FromForm(Name = "oficio")] IFormFile? oficio,
        ICommandHandler<CrearSolicitudAperturaCommand, SolicitudAperturaResponse> handler,
        CancellationToken cancellationToken)
    {
        var command = new CrearSolicitudAperturaCommand(
            experienciaEducativaId ?? 0,
            periodoEscolarId ?? 0,
            seccion,
            cantidadEstudiantes ?? 0,
            justificacion,
            oficio.ComoArchivoRecibido());
        var resultado = await handler.HandleAsync(command, cancellationToken);

        return resultado.IsSuccess
            ? TypedResults.CreatedAtRoute(resultado.Value, NombreRutaObtener, new { id = resultado.Value.Id })
            : resultado.Error.ToProblem();
    }
}

internal sealed record ListarSolicitudesAperturaRequest(
    [FromQuery(Name = "pagina")] int? Pagina,
    [FromQuery(Name = "tamanoPagina")] int? TamanoPagina,
    [FromQuery(Name = "estado")] string? Estado,
    [FromQuery(Name = "periodoEscolarId")] int? PeriodoEscolarId,
    [FromQuery(Name = "experienciaEducativaId")] int? ExperienciaEducativaId,
    [FromQuery(Name = "entidadAcademicaId")] int? EntidadAcademicaId)
{
    public Paginacion ComoPaginacion() =>
        new(Pagina ?? 1, TamanoPagina ?? Paginacion.TamanoPorOmision);
}
