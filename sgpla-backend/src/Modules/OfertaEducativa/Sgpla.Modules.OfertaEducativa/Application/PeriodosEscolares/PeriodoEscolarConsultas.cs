using Sgpla.Modules.OfertaEducativa.Domain.PeriodosEscolares;

namespace Sgpla.Modules.OfertaEducativa.Application.PeriodosEscolares;

internal sealed record PeriodoEscolarResponse(int Id, string Clave, DateOnly FechaInicio, DateOnly FechaFin)
{
    public static PeriodoEscolarResponse Desde(PeriodoEscolar periodo) =>
        new(periodo.Id, periodo.Clave, periodo.FechaInicio, periodo.FechaFin);
}

/// <summary>Todos los periodos activos, del más reciente al más antiguo (clave descendente).</summary>
internal sealed record ListarPeriodosEscolaresQuery;

internal sealed record ObtenerPeriodoEscolarQuery(int Id);
