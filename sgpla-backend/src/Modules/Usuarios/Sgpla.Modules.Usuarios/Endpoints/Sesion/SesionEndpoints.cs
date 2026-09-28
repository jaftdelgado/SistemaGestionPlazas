using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.Usuarios.Application.Sesion;

namespace Sgpla.Modules.Usuarios.Endpoints.Sesion;

internal static class SesionEndpoints
{
    public static RouteGroupBuilder MapSesionEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup(string.Empty).WithTags("Sesión");

        grupo.MapPost("/iniciar-sesion", IniciarSesion).WithName("IniciarSesion").AllowAnonymous()
            .WithSummary("Inicia sesión con el correo (o el usuario UV) y la contraseña.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        grupo.MapGet("/sesion", ObtenerSesion).WithName("ObtenerSesion").RequireAuthorization(Politicas.SesionIniciada)
            .WithSummary("Obtiene la cuenta de la sesión en curso.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        grupo.MapPost("/sesion/cambiar-contrasena", CambiarContrasena).WithName("CambiarContrasena")
            .RequireAuthorization(Politicas.SesionIniciada)
            .WithSummary("Cambia la contraseña del Superusuario en sesión y emite un token nuevo.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapPost("/cerrar-sesion", CerrarSesion).WithName("CerrarSesion").RequireAuthorization(Politicas.SesionIniciada)
            .WithSummary("Cierra la sesión; el token se descarta en el cliente.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return modulo;
    }

    private static async Task<Results<Ok<SesionResponse>, ProblemHttpResult>> IniciarSesion(
        IniciarSesionCommand command,
        ICommandHandler<IniciarSesionCommand, SesionIniciada> handler,
        IQueryHandler<ObtenerUsuarioSesionQuery, UsuarioSesionResponse> consulta,
        CancellationToken cancellationToken)
    {
        var resultado = await handler.HandleAsync(command, cancellationToken);
        return resultado.IsFailure
            ? resultado.Error.ToProblem()
            : await ArmarSesionAsync(resultado.Value, consulta, cancellationToken);
    }

    private static async Task<Results<Ok<UsuarioSesionResponse>, ProblemHttpResult>> ObtenerSesion(
        ICurrentUser usuarioActual,
        IQueryHandler<ObtenerUsuarioSesionQuery, UsuarioSesionResponse> consulta,
        CancellationToken cancellationToken) =>
        (await consulta.HandleAsync(new ObtenerUsuarioSesionQuery(usuarioActual.Id), cancellationToken)).ToOk();

    private static async Task<Results<Ok<SesionResponse>, ProblemHttpResult>> CambiarContrasena(
        CambiarContrasenaCommand command,
        ICommandHandler<CambiarContrasenaCommand, SesionIniciada> handler,
        IQueryHandler<ObtenerUsuarioSesionQuery, UsuarioSesionResponse> consulta,
        CancellationToken cancellationToken)
    {
        var resultado = await handler.HandleAsync(command, cancellationToken);
        return resultado.IsFailure
            ? resultado.Error.ToProblem()
            : await ArmarSesionAsync(resultado.Value, consulta, cancellationToken);
    }

    /// <summary>El cierre de sesión no tiene efecto en el servidor (Modulo_Usuarios.md, decisión D4).</summary>
    private static NoContent CerrarSesion() => TypedResults.NoContent();

    private static async Task<Results<Ok<SesionResponse>, ProblemHttpResult>> ArmarSesionAsync(
        SesionIniciada sesion,
        IQueryHandler<ObtenerUsuarioSesionQuery, UsuarioSesionResponse> consulta,
        CancellationToken cancellationToken)
    {
        var usuario = await consulta.HandleAsync(new ObtenerUsuarioSesionQuery(sesion.UsuarioId), cancellationToken);
        if (usuario.IsFailure)
        {
            return usuario.Error.ToProblem();
        }

        return TypedResults.Ok(new SesionResponse(sesion.Token.Token, sesion.Token.ExpiraEn, usuario.Value));
    }
}
