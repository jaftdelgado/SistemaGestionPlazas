using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.SolicitudesApertura.Application.Periodos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.SolicitudesApertura.Infrastructure.Periodos;

internal sealed class ObtenerPeriodosSolicitudAperturaHandler(
    IPeriodosConfigurados configurados,
    IPeriodosEscolares periodosEscolares)
    : IQueryHandler<ObtenerPeriodosSolicitudAperturaQuery, PeriodosSolicitudAperturaResponse>
{
    public async Task<Result<PeriodosSolicitudAperturaResponse>> HandleAsync(
        ObtenerPeriodosSolicitudAperturaQuery query,
        CancellationToken cancellationToken)
    {
        var activos = await periodosEscolares.ObtenerActivosPorClaveAsync(
            [configurados.ClaveActual, configurados.ClaveSiguiente], cancellationToken);

        return new PeriodosSolicitudAperturaResponse(
            Crear(configurados.ClaveActual, activos),
            Crear(configurados.ClaveSiguiente, activos));
    }

    private static PeriodoConfiguradoResponse Crear(
        string clave,
        IReadOnlyDictionary<string, PeriodoEscolarResumen> activos) =>
        activos.TryGetValue(clave, out var periodo)
            ? new PeriodoConfiguradoResponse(clave, periodo.Id, periodo.FechaInicio, periodo.FechaFin)
            : new PeriodoConfiguradoResponse(clave, null, null, null);
}
