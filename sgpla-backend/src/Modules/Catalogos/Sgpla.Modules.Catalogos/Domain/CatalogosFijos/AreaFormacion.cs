namespace Sgpla.Modules.Catalogos.Domain.CatalogosFijos;

/// <summary>Áreas de formación de las experiencias educativas; distintas del área académica.</summary>
internal sealed class AreaFormacion : CatalogoFijo
{
    public const int LongitudMaximaNombre = 200;
    public const int LongitudMaximaClave = 50;

    private AreaFormacion()
    {
    }

    public string Clave { get; private set; } = string.Empty;
}
