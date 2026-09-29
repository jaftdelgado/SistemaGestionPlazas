namespace Sgpla.Modules.Catalogos.Domain.CatalogosFijos;

/// <summary>Niveles de formación de los programas educativos (DATABASE.md §6.7).</summary>
internal sealed class NivelFormacion : CatalogoFijo
{
    public const int LongitudMaximaNombre = 200;
    public const int LongitudMaximaClave = 50;

    private NivelFormacion()
    {
    }

    public string Clave { get; private set; } = string.Empty;
}
