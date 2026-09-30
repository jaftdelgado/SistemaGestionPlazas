namespace Sgpla.Modules.Catalogos.Domain.CatalogosFijos;

/// <summary>Tratamientos académicos, cada uno ligado a un grado académico.</summary>
internal sealed class TratamientoAcademico : CatalogoFijo
{
    public const int LongitudMaximaNombre = 30;

    private TratamientoAcademico()
    {
    }

    public int GradoAcademicoId { get; private set; }
}
