using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.Catalogos.Application.GradosAcademicos;
using Sgpla.Modules.Catalogos.Application.GradosAcademicos.Listar;
using Sgpla.Modules.Catalogos.Application.GradosAcademicos.Obtener;

namespace Sgpla.Modules.Catalogos.Endpoints.GradosAcademicos;

/// <summary>Catálogo fijo: solo lectura (ESTANDAR_MODULOS.md, "Catálogos fijos").</summary>
internal static class GradoAcademicoEndpoints
{
    public static RouteGroupBuilder MapGradoAcademicoEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/grados-academicos").WithTags("Grados académicos");

        grupo.MapGet("/", Listar).WithName("ListarGradosAcademicos")
            .WithSummary("Lista todos los grados académicos, en orden de jerarquía.");
        grupo.MapGet("/{id:int}", Obtener).WithName("ObtenerGradoAcademico").WithSummary("Obtiene un grado académico.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return modulo;
    }

    private static async Task<Results<Ok<IReadOnlyList<GradoAcademicoResponse>>, ProblemHttpResult>> Listar(
        IQueryHandler<ListarGradosAcademicosQuery, IReadOnlyList<GradoAcademicoResponse>> handler,
        CancellationToken cancellationToken)
    {
        var resultado = await handler.HandleAsync(new ListarGradosAcademicosQuery(), cancellationToken);

        return resultado.IsSuccess ? TypedResults.Ok(resultado.Value) : resultado.Error.ToProblem();
    }

    private static async Task<Results<Ok<GradoAcademicoResponse>, ProblemHttpResult>> Obtener(
        int id,
        IQueryHandler<ObtenerGradoAcademicoQuery, GradoAcademicoResponse> handler,
        CancellationToken cancellationToken)
    {
        var resultado = await handler.HandleAsync(new ObtenerGradoAcademicoQuery(id), cancellationToken);

        return resultado.IsSuccess ? TypedResults.Ok(resultado.Value) : resultado.Error.ToProblem();
    }
}
