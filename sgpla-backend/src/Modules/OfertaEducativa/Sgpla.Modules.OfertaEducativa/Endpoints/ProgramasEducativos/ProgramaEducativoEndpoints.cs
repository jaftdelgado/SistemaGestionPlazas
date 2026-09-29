using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.OfertaEducativa.Application.ProgramasEducativos;

namespace Sgpla.Modules.OfertaEducativa.Endpoints.ProgramasEducativos;

internal static class ProgramaEducativoEndpoints
{
    private const string NombreRutaObtener = "ObtenerProgramaEducativo";

    public static RouteGroupBuilder MapProgramaEducativoEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/programas-educativos").WithTags("Programas educativos");

        grupo.MapGet("/", Listar).WithName("ListarProgramasEducativos")
            .WithSummary("Lista los programas educativos activos de tu ámbito, paginados y con filtros opcionales.")
            .ProducesValidationProblem();
        grupo.MapGet("/{id:int}", Obtener).WithName(NombreRutaObtener)
            .WithSummary("Obtiene un programa educativo activo de tu ámbito.")
            .ProducesProblem(StatusCodes.Status404NotFound);
        grupo.MapPost("/", Crear).WithName("CrearProgramaEducativo")
            .WithSummary("Registra un programa educativo en una entidad académica de tu área.")
            .RequireAuthorization(Politicas.Dgaa)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
        grupo.MapPut("/{id:int}", Modificar).WithName("ModificarProgramaEducativo")
            .WithSummary("Modifica el nombre, el sistema educativo y el nivel de un programa; la entidad no cambia.")
            .RequireAuthorization(Politicas.Dgaa)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        grupo.MapDelete("/{id:int}", DarDeBaja).WithName("DarDeBajaProgramaEducativo")
            .WithSummary("Da de baja un programa educativo sin planes de estudio activos.")
            .RequireAuthorization(Politicas.Dgaa)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return modulo;
    }

    private static async Task<Results<Ok<Pagina<ProgramaEducativoResponse>>, ProblemHttpResult>> Listar(
        [AsParameters] ListarProgramasEducativosRequest request,
        IQueryHandler<ListarProgramasEducativosQuery, Pagina<ProgramaEducativoResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(
            new ListarProgramasEducativosQuery(request.ComoPaginacion(), request.ComoFiltros()), cancellationToken))
            .ToOk();

    private static async Task<Results<Ok<ProgramaEducativoResponse>, ProblemHttpResult>> Obtener(
        int id,
        IQueryHandler<ObtenerProgramaEducativoQuery, ProgramaEducativoResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ObtenerProgramaEducativoQuery(id), cancellationToken)).ToOk();

    /// <summary>El cuerpo coincide con el comando, así que se enlaza directamente sin un request propio.</summary>
    private static async Task<Results<CreatedAtRoute<ProgramaEducativoResponse>, ProblemHttpResult>> Crear(
        CrearProgramaEducativoCommand command,
        ICommandHandler<CrearProgramaEducativoCommand, int> comandoHandler,
        IQueryHandler<ObtenerProgramaEducativoQuery, ProgramaEducativoResponse> consultaHandler,
        CancellationToken cancellationToken)
    {
        var creado = await comandoHandler.HandleAsync(command, cancellationToken);
        if (creado.IsFailure)
        {
            return creado.Error.ToProblem();
        }

        var respuesta = await consultaHandler.HandleAsync(new ObtenerProgramaEducativoQuery(creado.Value), cancellationToken);
        return TypedResults.CreatedAtRoute(respuesta.Value, NombreRutaObtener, new { id = creado.Value });
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Modificar(
        int id,
        ModificarProgramaEducativoRequest request,
        ICommandHandler<ModificarProgramaEducativoCommand> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(
            new ModificarProgramaEducativoCommand(id, request.Nombre, request.SistemaEducativoId, request.NivelFormacionId),
            cancellationToken))
            .ToNoContent();

    private static async Task<Results<NoContent, ProblemHttpResult>> DarDeBaja(
        int id,
        ICommandHandler<DarDeBajaProgramaEducativoCommand> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new DarDeBajaProgramaEducativoCommand(id), cancellationToken)).ToNoContent();
}

/// <summary>Parámetros de consulta del listado; cada uno llega en camelCase, como lo envía el frontend.</summary>
internal sealed record ListarProgramasEducativosRequest(
    [FromQuery(Name = "pagina")] int? Pagina,
    [FromQuery(Name = "tamanoPagina")] int? TamanoPagina,
    [FromQuery(Name = "entidadAcademicaId")] int? EntidadAcademicaId,
    [FromQuery(Name = "sistemaEducativoId")] int? SistemaEducativoId,
    [FromQuery(Name = "nivelFormacionId")] int? NivelFormacionId,
    [FromQuery(Name = "busqueda")] string? Busqueda)
{
    public Paginacion ComoPaginacion() => new(Pagina ?? 1, TamanoPagina ?? Paginacion.TamanoPorOmision);

    public FiltrosProgramasEducativos ComoFiltros() =>
        new(EntidadAcademicaId, SistemaEducativoId, NivelFormacionId, Busqueda);
}

internal sealed record ModificarProgramaEducativoRequest(string Nombre, int SistemaEducativoId, int NivelFormacionId);
