using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.Modules.SolicitudesApertura.Application.Periodos;

namespace Sgpla.Modules.SolicitudesApertura.Endpoints.Periodos;

internal static class PeriodoEndpoints
{
    public static RouteGroupBuilder MapPeriodoEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/periodos").WithTags("Periodos de solicitud");
        grupo.MapGet("/", Obtener).WithName("ObtenerPeriodosSolicitudApertura")
            .WithSummary("Obtiene el periodo actual y el siguiente configurados para las solicitudes de apertura.");
        return modulo;
    }

    private static async Task<Ok<PeriodosSolicitudAperturaResponse>> Obtener(
        IQueryHandler<ObtenerPeriodosSolicitudAperturaQuery, PeriodosSolicitudAperturaResponse> handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok((await handler.HandleAsync(new ObtenerPeriodosSolicitudAperturaQuery(), cancellationToken)).Value);
}
