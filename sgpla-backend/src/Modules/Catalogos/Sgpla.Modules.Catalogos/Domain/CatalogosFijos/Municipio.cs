namespace Sgpla.Modules.Catalogos.Domain.CatalogosFijos;

/// <summary>Municipios de Veracruz. El id es la clave municipal del INEGI.</summary>
internal sealed class Municipio : CatalogoFijo
{
    public const int LongitudMaximaNombre = 150;

    private Municipio()
    {
    }
}
