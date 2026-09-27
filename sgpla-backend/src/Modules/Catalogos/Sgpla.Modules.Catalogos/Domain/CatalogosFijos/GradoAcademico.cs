namespace Sgpla.Modules.Catalogos.Domain.CatalogosFijos;

/// <summary>Grados académicos (DATABASE.md §6.21). El id sigue la jerarquía académica.</summary>
internal sealed class GradoAcademico : CatalogoFijo
{
    public const int LongitudMaximaNombre = 150;

    private GradoAcademico()
    {
    }
}
