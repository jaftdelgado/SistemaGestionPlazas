namespace Sgpla.Modules.Catalogos.Domain.CatalogosFijos;

/// <summary>Modalidades de recepción de documentos (DATABASE.md §15.2).</summary>
internal sealed class ModalidadRecepcion : CatalogoFijo
{
    public const int LongitudMaximaNombre = 100;

    private ModalidadRecepcion()
    {
    }

    /// <summary>Si quien usa la modalidad debe indicar un lugar de recepción.</summary>
    public bool RequiereLugar { get; private set; }
}
