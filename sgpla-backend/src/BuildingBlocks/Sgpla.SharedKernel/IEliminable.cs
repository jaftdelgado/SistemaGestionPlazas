namespace Sgpla.SharedKernel;

/// <summary>Entidad con baja lógica: <see cref="FechaEliminacion"/> en UTC; <c>null</c> significa activa.</summary>
public interface IEliminable
{
    DateTime? FechaEliminacion { get; }
}
