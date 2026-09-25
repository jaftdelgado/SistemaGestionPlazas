using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Domain.GradosAcademicos;

/// <summary>
/// Catálogo fijo de grados académicos (DATABASE.md §6.21). Sus valores los carga la semilla con ids estables y
/// no admiten altas, modificaciones ni bajas durante la operación, así que la entidad no tiene comportamiento.
/// </summary>
internal sealed class GradoAcademico : Entity
{
    public const int LongitudMaximaNombre = 150;

    private GradoAcademico()
    {
    }

    public string Nombre { get; private set; } = string.Empty;
}
