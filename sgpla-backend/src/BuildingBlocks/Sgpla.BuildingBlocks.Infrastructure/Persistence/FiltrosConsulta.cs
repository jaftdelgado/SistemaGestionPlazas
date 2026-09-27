namespace Sgpla.BuildingBlocks.Infrastructure.Persistence;

/// <summary>Nombres de los filtros de consulta globales de EF Core.</summary>
public static class FiltrosConsulta
{
    /// <summary>Oculta las filas con <c>fecha_eliminacion</c>. Se omite con <c>IgnoreQueryFilters([BajaLogica])</c>.</summary>
    public const string BajaLogica = nameof(BajaLogica);
}
