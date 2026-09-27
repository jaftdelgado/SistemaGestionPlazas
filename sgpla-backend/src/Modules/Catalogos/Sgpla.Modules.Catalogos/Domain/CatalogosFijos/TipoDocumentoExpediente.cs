namespace Sgpla.Modules.Catalogos.Domain.CatalogosFijos;

/// <summary>Tipos de documento del expediente (DATABASE.md §6.22).</summary>
internal sealed class TipoDocumentoExpediente : CatalogoFijo
{
    public const int LongitudMaximaNombre = 150;

    private TipoDocumentoExpediente()
    {
    }
}
