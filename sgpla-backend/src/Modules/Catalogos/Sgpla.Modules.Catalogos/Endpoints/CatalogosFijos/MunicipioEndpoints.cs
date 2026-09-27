using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.Catalogos.Application.CatalogosFijos;

namespace Sgpla.Modules.Catalogos.Endpoints.CatalogosFijos;

/// <summary>Catálogo fijo paginado y con búsqueda por nombre: lo consume un Select del frontend.</summary>
internal static partial class MunicipioEndpoints
{
    public static RouteGroupBuilder MapMunicipioEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/municipios").WithTags("Municipios");

        grupo.MapGet("/", Listar).WithName("ListarMunicipios")
            .WithSummary("Lista los municipios, en orden alfabético y paginados; con busqueda, solo los que la contienen.")
            .ProducesValidationProblem();
        grupo.MapGet("/{id:int}", Obtener).WithName("ObtenerMunicipio")
            .WithSummary("Obtiene un municipio.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return modulo;
    }

    private static async Task<Results<Ok<Pagina<CatalogoFijoResponse>>, ProblemHttpResult>> Listar(
        [AsParameters] ListarMunicipiosRequest request,
        IQueryHandler<ListarMunicipiosQuery, Pagina<CatalogoFijoResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(
            new ListarMunicipiosQuery(request.ComoPaginacion(), NormalizarBusqueda(request.Busqueda)), cancellationToken))
            .ToOk();

    private static async Task<Results<Ok<CatalogoFijoResponse>, ProblemHttpResult>> Obtener(
        int id,
        IQueryHandler<ObtenerMunicipioQuery, CatalogoFijoResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ObtenerMunicipioQuery(id), cancellationToken)).ToOk();

    /// <summary>"  poza   rica " → "poza rica": recorta y colapsa espacios repetidos. Vacía queda en <c>null</c>.</summary>
    private static string? NormalizarBusqueda(string? busqueda)
    {
        if (busqueda is null)
        {
            return null;
        }

        var normalizada = EspaciosRepetidos().Replace(busqueda.Trim(), " ");
        return normalizada.Length == 0 ? null : normalizada;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspaciosRepetidos();
}

/// <summary>Parámetros de consulta del listado: <c>?pagina=1&amp;tamanoPagina=20&amp;busqueda=xalapa</c>.</summary>
internal sealed record ListarMunicipiosRequest(
    [FromQuery(Name = "pagina")] int? Pagina,
    [FromQuery(Name = "tamanoPagina")] int? TamanoPagina,
    [FromQuery(Name = "busqueda")] string? Busqueda)
{
    public Paginacion ComoPaginacion() => new(Pagina ?? 1, TamanoPagina ?? Paginacion.TamanoPorOmision);
}
