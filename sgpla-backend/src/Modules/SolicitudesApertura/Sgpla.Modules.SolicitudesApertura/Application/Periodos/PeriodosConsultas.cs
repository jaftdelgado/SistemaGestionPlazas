namespace Sgpla.Modules.SolicitudesApertura.Application.Periodos;

internal sealed record PeriodosSolicitudAperturaResponse(
    PeriodoConfiguradoResponse Actual,
    PeriodoConfiguradoResponse Siguiente);

/// <summary>Id y fechas solo si el periodo existe activo; si no, <c>null</c>.</summary>
internal sealed record PeriodoConfiguradoResponse(string Clave, int? Id, DateOnly? FechaInicio, DateOnly? FechaFin);

internal sealed record ObtenerPeriodosSolicitudAperturaQuery;
