using Sgpla.SharedKernel;

namespace Sgpla.Modules.Institucional.Domain.Ubicaciones;

/// <summary>Regiones universitarias de la UV (DATABASE.md §6.1). Solo lectura: los valores los carga la semilla.</summary>
internal sealed class Region : Entity
{
    public const int LongitudMaximaNombre = 200;

    private Region()
    {
    }

    public int Clave { get; private set; }

    public string Nombre { get; private set; } = string.Empty;
}
