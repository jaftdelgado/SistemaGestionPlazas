using Sgpla.SharedKernel;

namespace Sgpla.Modules.Institucional.Domain.Ubicaciones;

/// <summary>Campus de la UV. Solo lectura: los valores los carga la semilla.</summary>
internal sealed class Campus : Entity
{
    public const int LongitudMaximaClave = 50;
    public const int LongitudMaximaNombre = 200;

    private Campus()
    {
    }

    public string Clave { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public int RegionId { get; private set; }
}
