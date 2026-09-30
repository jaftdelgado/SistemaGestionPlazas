using System.Globalization;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;

internal static class ArchivoSolicitudAperturaErrors
{
    public static Error Obligatorio => Error.Validation(
        "ArchivoSolicitudApertura.Obligatorio", "El oficio de respaldo es obligatorio.", "Oficio");

    public static Error NombreVacio => Error.Validation(
        "ArchivoSolicitudApertura.NombreVacio", "El archivo del oficio debe tener nombre.", "Oficio");

    public static Error NombreDemasiadoLargo => Error.Validation(
        "ArchivoSolicitudApertura.NombreDemasiadoLargo",
        $"El nombre del archivo del oficio admite hasta {ArchivoSolicitudApertura.LongitudMaximaNombre} caracteres.",
        "Oficio");

    public static Error Vacio => Error.Validation(
        "ArchivoSolicitudApertura.Vacio", "El archivo del oficio está vacío.", "Oficio");

    public static Error DemasiadoGrande(long tamanoMaximoBytes) => Error.Validation(
        "ArchivoSolicitudApertura.DemasiadoGrande",
        $"El oficio supera el tamaño máximo de {tamanoMaximoBytes.ToString(CultureInfo.InvariantCulture)} bytes.",
        "Oficio");

    public static Error NoEsPdf => Error.Validation(
        "ArchivoSolicitudApertura.NoEsPdf", "El oficio debe ser un archivo PDF.", "Oficio");
}
