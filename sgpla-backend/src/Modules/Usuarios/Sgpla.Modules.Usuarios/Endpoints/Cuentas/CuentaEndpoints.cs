using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.Usuarios.Application.Cuentas;

namespace Sgpla.Modules.Usuarios.Endpoints.Cuentas;

internal static class CuentaEndpoints
{
    private const string NombreRutaObtener = "ObtenerCuenta";

    public static RouteGroupBuilder MapCuentaEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/cuentas").WithTags("Cuentas").RequireAuthorization(Politicas.Superusuario);
        grupo.ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden);

        grupo.MapGet("/", Listar).WithName("ListarCuentas")
            .WithSummary("Lista las cuentas activas, paginadas y con filtros opcionales.")
            .ProducesValidationProblem();
        grupo.MapGet("/{id:int}", Obtener).WithName(NombreRutaObtener)
            .WithSummary("Obtiene una cuenta activa.")
            .ProducesProblem(StatusCodes.Status404NotFound);
        grupo.MapPost("/", Crear).WithName("CrearCuenta")
            .WithSummary("Registra una cuenta; la de un Superusuario recibe una contraseña temporal.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
        grupo.MapPut("/{id:int}", Modificar).WithName("ModificarCuenta")
            .WithSummary("Modifica el nombre de una cuenta.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
        grupo.MapPost("/{id:int}/restablecer-contrasena", RestablecerContrasena).WithName("RestablecerContrasena")
            .WithSummary("Asigna una contraseña temporal nueva a otro Superusuario.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        grupo.MapDelete("/{id:int}", DarDeBaja).WithName("DarDeBajaCuenta")
            .WithSummary("Da de baja una cuenta que no es la propia.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return modulo;
    }

    private static async Task<Results<Ok<Pagina<CuentaResponse>>, ProblemHttpResult>> Listar(
        [AsParameters] ListarCuentasRequest request,
        IQueryHandler<ListarCuentasQuery, Pagina<CuentaResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListarCuentasQuery(request.ComoPaginacion(), request.ComoFiltros()), cancellationToken))
            .ToOk();

    private static async Task<Results<Ok<CuentaResponse>, ProblemHttpResult>> Obtener(
        int id,
        IQueryHandler<ObtenerCuentaQuery, CuentaResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ObtenerCuentaQuery(id), cancellationToken)).ToOk();

    private static async Task<Results<CreatedAtRoute<CuentaCreadaResponse>, ProblemHttpResult>> Crear(
        CrearCuentaCommand command,
        ICommandHandler<CrearCuentaCommand, CuentaCreada> comandoHandler,
        IQueryHandler<ObtenerCuentaQuery, CuentaResponse> consultaHandler,
        CancellationToken cancellationToken)
    {
        var creada = await comandoHandler.HandleAsync(command, cancellationToken);
        if (creada.IsFailure)
        {
            return creada.Error.ToProblem();
        }

        var cuenta = await consultaHandler.HandleAsync(new ObtenerCuentaQuery(creada.Value.Id), cancellationToken);
        var respuesta = new CuentaCreadaResponse(cuenta.Value, creada.Value.ContrasenaTemporal);
        return TypedResults.CreatedAtRoute(respuesta, NombreRutaObtener, new { id = creada.Value.Id });
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Modificar(
        int id,
        ModificarCuentaRequest request,
        ICommandHandler<ModificarCuentaCommand> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ModificarCuentaCommand(id, request.Nombre), cancellationToken)).ToNoContent();

    private static async Task<Results<Ok<ContrasenaTemporalResponse>, ProblemHttpResult>> RestablecerContrasena(
        int id,
        ICommandHandler<RestablecerContrasenaCommand, string> handler,
        CancellationToken cancellationToken)
    {
        var resultado = await handler.HandleAsync(new RestablecerContrasenaCommand(id), cancellationToken);
        return resultado.IsFailure
            ? resultado.Error.ToProblem()
            : TypedResults.Ok(new ContrasenaTemporalResponse(resultado.Value));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DarDeBaja(
        int id,
        ICommandHandler<DarDeBajaCuentaCommand> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new DarDeBajaCuentaCommand(id), cancellationToken)).ToNoContent();
}

/// <summary>Parámetros de consulta del listado; cada uno llega en camelCase, como lo envía el frontend.</summary>
internal sealed record ListarCuentasRequest(
    [FromQuery(Name = "pagina")] int? Pagina,
    [FromQuery(Name = "tamanoPagina")] int? TamanoPagina,
    [FromQuery(Name = "rolId")] byte? RolId,
    [FromQuery(Name = "areaAcademicaId")] int? AreaAcademicaId,
    [FromQuery(Name = "entidadAcademicaId")] int? EntidadAcademicaId,
    [FromQuery(Name = "busqueda")] string? Busqueda)
{
    public Paginacion ComoPaginacion() => new(Pagina ?? 1, TamanoPagina ?? Paginacion.TamanoPorOmision);

    public FiltrosCuentas ComoFiltros() => new(RolId, AreaAcademicaId, EntidadAcademicaId, Busqueda);
}

internal sealed record ModificarCuentaRequest(string Nombre);

internal sealed record CuentaCreadaResponse(CuentaResponse Cuenta, string? ContrasenaTemporal);

internal sealed record ContrasenaTemporalResponse(string ContrasenaTemporal);
