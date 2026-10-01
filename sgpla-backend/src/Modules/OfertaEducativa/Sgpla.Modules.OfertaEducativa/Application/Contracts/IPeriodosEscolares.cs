namespace Sgpla.Modules.OfertaEducativa.Application.Contracts;

/// <summary>Consulta de periodos escolares para otros módulos (SolicitudesApertura).</summary>
public interface IPeriodosEscolares
{
    /// <summary>Incluye los dados de baja (<c>Activo = false</c>). Los inexistentes no aparecen. Con una colección vacía no se consulta la base.</summary>
    Task<IReadOnlyDictionary<int, PeriodoEscolarResumen>> ObtenerAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken);

    /// <summary>Solo activos: una clave sin periodo activo no aparece. Con una colección vacía no se consulta la base.</summary>
    Task<IReadOnlyDictionary<string, PeriodoEscolarResumen>> ObtenerActivosPorClaveAsync(
        IReadOnlyCollection<string> claves,
        CancellationToken cancellationToken);
}

public sealed record PeriodoEscolarResumen(int Id, string Clave, DateOnly FechaInicio, DateOnly FechaFin, bool Activo);
