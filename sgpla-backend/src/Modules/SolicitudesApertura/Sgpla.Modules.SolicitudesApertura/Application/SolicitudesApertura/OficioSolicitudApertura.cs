using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;

internal static class OficioSolicitudApertura
{
    public static Result<string> Validar(ArchivoRecibido? oficio, long tamanoMaximoBytes)
    {
        if (oficio is null)
        {
            return ArchivoSolicitudAperturaErrors.Obligatorio;
        }

        var nombre = oficio.Nombre ?? string.Empty;
        var separador = Math.Max(nombre.LastIndexOf('/'), nombre.LastIndexOf('\\'));
        nombre = Normalizacion.Recortar(nombre[(separador + 1)..]);

        if (nombre.Length == 0)
        {
            return ArchivoSolicitudAperturaErrors.NombreVacio;
        }

        if (nombre.Length > ArchivoSolicitudApertura.LongitudMaximaNombre)
        {
            return ArchivoSolicitudAperturaErrors.NombreDemasiadoLargo;
        }

        if (oficio.Tamano <= 0)
        {
            return ArchivoSolicitudAperturaErrors.Vacio;
        }

        if (oficio.Tamano > tamanoMaximoBytes)
        {
            return ArchivoSolicitudAperturaErrors.DemasiadoGrande(tamanoMaximoBytes);
        }

        if (!string.Equals(oficio.TipoContenido, ArchivoSolicitudApertura.MimePdf, StringComparison.OrdinalIgnoreCase))
        {
            return ArchivoSolicitudAperturaErrors.NoEsPdf;
        }

        return nombre;
    }

    public static bool TieneFirmaPdf(ReadOnlySpan<byte> encabezado) =>
        encabezado.Length >= 5 && encabezado[..5].SequenceEqual("%PDF-"u8);
}
