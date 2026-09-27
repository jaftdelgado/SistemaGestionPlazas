using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.Institucional.Application.EntidadesAcademicas;

namespace Sgpla.Modules.Institucional.Endpoints.EntidadesAcademicas;

internal static class EntidadAcademicaEndpoints
{
    private const string NombreRutaObtener = "ObtenerEntidadAcademica";

    public static RouteGroupBuilder MapEntidadAcademicaEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/entidades-academicas").WithTags("Entidades académicas");
        // Autorización: cuando exista JWT (módulo Usuarios), la lectura se filtra por ámbito (DGAA: las de su
        // área; Entidad Académica: solo la suya) y la escritura es solo del Superusuario (Modulo_Institucional.md,
        // decisión D7).

        grupo.MapGet("/", Listar).WithName("ListarEntidadesAcademicas")
            .WithSummary("Lista las entidades académicas activas, paginadas y con filtros opcionales.")
            .ProducesValidationProblem();
        grupo.MapGet("/{id:int}", Obtener).WithName(NombreRutaObtener)
            .WithSummary("Obtiene una entidad académica activa.")
            .ProducesProblem(StatusCodes.Status404NotFound);
        grupo.MapPost("/", Crear).WithName("CrearEntidadAcademica")
            .WithSummary("Registra una entidad académica.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
        grupo.MapPut("/{id:int}", Modificar).WithName("ModificarEntidadAcademica")
            .WithSummary("Modifica los datos editables de una entidad académica; la clave y el campus no cambian.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
        grupo.MapDelete("/{id:int}", DarDeBaja).WithName("DarDeBajaEntidadAcademica")
            .WithSummary("Da de baja una entidad académica.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return modulo;
    }

    private static async Task<Results<Ok<Pagina<EntidadAcademicaResponse>>, ProblemHttpResult>> Listar(
        [AsParameters] ListarEntidadesAcademicasRequest request,
        IQueryHandler<ListarEntidadesAcademicasQuery, Pagina<EntidadAcademicaResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(
            new ListarEntidadesAcademicasQuery(request.ComoPaginacion(), request.ComoFiltros()), cancellationToken))
            .ToOk();

    private static async Task<Results<Ok<EntidadAcademicaResponse>, ProblemHttpResult>> Obtener(
        int id,
        IQueryHandler<ObtenerEntidadAcademicaQuery, EntidadAcademicaResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ObtenerEntidadAcademicaQuery(id), cancellationToken)).ToOk();

    /// <summary>El cuerpo coincide con el comando, así que se enlaza directamente sin un request propio.</summary>
    private static async Task<Results<CreatedAtRoute<EntidadAcademicaResponse>, ProblemHttpResult>> Crear(
        CrearEntidadAcademicaCommand command,
        ICommandHandler<CrearEntidadAcademicaCommand, int> comandoHandler,
        IQueryHandler<ObtenerEntidadAcademicaQuery, EntidadAcademicaResponse> consultaHandler,
        CancellationToken cancellationToken)
    {
        var creada = await comandoHandler.HandleAsync(command, cancellationToken);
        if (creada.IsFailure)
        {
            return creada.Error.ToProblem();
        }

        var respuesta = await consultaHandler.HandleAsync(new ObtenerEntidadAcademicaQuery(creada.Value), cancellationToken);
        return TypedResults.CreatedAtRoute(respuesta.Value, NombreRutaObtener, new { id = creada.Value });
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Modificar(
        int id,
        ModificarEntidadAcademicaRequest request,
        ICommandHandler<ModificarEntidadAcademicaCommand> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(
            new ModificarEntidadAcademicaCommand(
                id,
                request.Nombre,
                request.Calle,
                request.NumeroExterior,
                request.Colonia,
                request.CodigoPostal,
                request.Telefono,
                request.Extension,
                request.AreaAcademicaId,
                request.MunicipioId),
            cancellationToken))
            .ToNoContent();

    private static async Task<Results<NoContent, ProblemHttpResult>> DarDeBaja(
        int id,
        ICommandHandler<DarDeBajaEntidadAcademicaCommand> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new DarDeBajaEntidadAcademicaCommand(id), cancellationToken)).ToNoContent();
}

/// <summary>Parámetros de consulta del listado; cada uno llega en camelCase, como lo envía el frontend.</summary>
internal sealed record ListarEntidadesAcademicasRequest(
    [FromQuery(Name = "pagina")] int? Pagina,
    [FromQuery(Name = "tamanoPagina")] int? TamanoPagina,
    [FromQuery(Name = "regionId")] int? RegionId,
    [FromQuery(Name = "campusId")] int? CampusId,
    [FromQuery(Name = "areaAcademicaId")] int? AreaAcademicaId,
    [FromQuery(Name = "municipioId")] int? MunicipioId,
    [FromQuery(Name = "busqueda")] string? Busqueda,
    [FromQuery(Name = "calle")] string? Calle,
    [FromQuery(Name = "colonia")] string? Colonia,
    [FromQuery(Name = "numeroExterior")] string? NumeroExterior,
    [FromQuery(Name = "codigoPostal")] string? CodigoPostal,
    [FromQuery(Name = "telefono")] string? Telefono)
{
    public Paginacion ComoPaginacion() => new(Pagina ?? 1, TamanoPagina ?? Paginacion.TamanoPorOmision);

    public FiltrosEntidadesAcademicas ComoFiltros() => new(
        RegionId, CampusId, AreaAcademicaId, MunicipioId, Busqueda, Calle, Colonia, NumeroExterior, CodigoPostal, Telefono);
}

internal sealed record ModificarEntidadAcademicaRequest(
    string Nombre,
    string Calle,
    string? NumeroExterior,
    string Colonia,
    string CodigoPostal,
    string Telefono,
    string? Extension,
    int AreaAcademicaId,
    int MunicipioId);
