using System.Globalization;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;

internal static class ArchivoSolicitudAperturaErrors
{
    public static readonly Error Obligatorio = Error.Validation(
        "ArchivoSolicitudApertura.Obligatorio", "El oficio de respaldo es obligatorio.", nameof(SolicitudApertura.Oficio));

    public static readonly Error NombreVacio = Error.Validation(
        "ArchivoSolicitudApertura.NombreVacio", "El archivo del oficio debe tener nombre.", nameof(SolicitudApertura.Oficio));

    public static readonly Error NombreDemasiadoLargo = Error.Validation(
        "ArchivoSolicitudApertura.NombreDemasiadoLargo",
        $"El nombre del archivo del oficio admite hasta {ArchivoSolicitudApertura.LongitudMaximaNombre} caracteres.",
        nameof(SolicitudApertura.Oficio));

    public static readonly Error Vacio = Error.Validation(
        "ArchivoSolicitudApertura.Vacio", "El archivo del oficio está vacío.", nameof(SolicitudApertura.Oficio));

    public static Error DemasiadoGrande(long tamanoMaximoBytes) => Error.Validation(
        "ArchivoSolicitudApertura.DemasiadoGrande",
        $"El oficio supera el tamaño máximo de {tamanoMaximoBytes.ToString(CultureInfo.InvariantCulture)} bytes.",
        nameof(SolicitudApertura.Oficio));

    public static readonly Error NoEsPdf = Error.Validation(
        "ArchivoSolicitudApertura.NoEsPdf", "El oficio debe ser un archivo PDF.", nameof(SolicitudApertura.Oficio));
}
