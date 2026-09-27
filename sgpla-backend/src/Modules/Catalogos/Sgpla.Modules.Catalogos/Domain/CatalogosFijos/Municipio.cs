namespace Sgpla.Modules.Catalogos.Domain.CatalogosFijos;

/// <summary>Municipios de Veracruz (DATABASE.md §6.4). El id es la clave municipal del INEGI.</summary>
internal sealed class Municipio : CatalogoFijo
{
    public const int LongitudMaximaNombre = 150;

    private Municipio()
    {
    }
}
