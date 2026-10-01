using Sgpla.SharedKernel;

namespace Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;

internal sealed class ArchivoSolicitudApertura : Entity
{
    public const int LongitudMaximaNombre = 260;
    public const string MimePdf = "application/pdf";
    public const int LongitudMaximaMime = 255;
    public const int LongitudChecksum = 32;
    public const int LongitudFirmaPdf = 5;
    public const int LongitudMaximaClave = 500;

    private ArchivoSolicitudApertura()
    {
    }

    public string Nombre { get; private set; } = string.Empty;

    public string Mime { get; private set; } = MimePdf;

    public long Tamano { get; private set; }

    public byte[] ChecksumSha256 { get; private set; } = [];

    public string ClaveAlmacenamiento { get; private set; } = string.Empty;

    public DateTime CargadoEn { get; private set; }

    public int CargadoPorUsuarioId { get; private set; }

    public static Result<string> ValidarOficio(string? nombre, string? tipoContenido, long tamano, long tamanoMaximoBytes)
    {
        var texto = nombre ?? string.Empty;
        var separador = Math.Max(texto.LastIndexOf('/'), texto.LastIndexOf('\\'));
        texto = Normalizacion.Recortar(texto[(separador + 1)..]);

        if (texto.Length == 0)
        {
            return ArchivoSolicitudAperturaErrors.NombreVacio;
        }

        if (texto.Length > LongitudMaximaNombre)
        {
            return ArchivoSolicitudAperturaErrors.NombreDemasiadoLargo;
        }

        if (tamano <= 0)
        {
            return ArchivoSolicitudAperturaErrors.Vacio;
        }

        if (tamano > tamanoMaximoBytes)
        {
            return ArchivoSolicitudAperturaErrors.DemasiadoGrande(tamanoMaximoBytes);
        }

        if (!string.Equals(tipoContenido, MimePdf, StringComparison.OrdinalIgnoreCase))
        {
            return ArchivoSolicitudAperturaErrors.NoEsPdf;
        }

        return texto;
    }

    public static bool TieneFirmaPdf(ReadOnlySpan<byte> encabezado) =>
        encabezado.Length >= LongitudFirmaPdf && encabezado[..LongitudFirmaPdf].SequenceEqual("%PDF-"u8);

    public static ArchivoSolicitudApertura Crear(
        string nombre,
        long tamano,
        byte[] checksumSha256,
        string claveAlmacenamiento,
        int usuarioId,
        DateTime utc) =>
        new()
        {
            Nombre = nombre,
            Mime = MimePdf,
            Tamano = tamano,
            ChecksumSha256 = checksumSha256,
            ClaveAlmacenamiento = claveAlmacenamiento,
            CargadoEn = utc,
            CargadoPorUsuarioId = usuarioId,
        };
}
