using Sgpla.SharedKernel;

namespace Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;

internal sealed record DatosSolicitudApertura(string Seccion, int CantidadEstudiantes, string Justificacion)
{
    public static Result<DatosSolicitudApertura> Crear(string? seccion, int cantidadEstudiantes, string? justificacion)
    {
        var seccionNormalizada = Normalizacion.Recortar(seccion).ToUpperInvariant();
        if (seccionNormalizada.Length == 0)
        {
            return SolicitudAperturaErrors.SeccionVacia;
        }

        if (seccionNormalizada.Length > SolicitudApertura.LongitudMaximaSeccion)
        {
            return SolicitudAperturaErrors.SeccionDemasiadoLarga;
        }

        if (!seccionNormalizada.All(c => c is (>= 'A' and <= 'Z') or (>= '0' and <= '9')))
        {
            return SolicitudAperturaErrors.SeccionFormatoInvalido;
        }

        if (cantidadEstudiantes <= 0)
        {
            return SolicitudAperturaErrors.CantidadNoPositiva;
        }

        var justificacionNormalizada = Normalizacion.Recortar(justificacion);
        if (justificacionNormalizada.Length == 0)
        {
            return SolicitudAperturaErrors.JustificacionVacia;
        }

        if (justificacionNormalizada.Length > SolicitudApertura.LongitudMaximaJustificacion)
        {
            return SolicitudAperturaErrors.JustificacionDemasiadoLarga;
        }

        return new DatosSolicitudApertura(seccionNormalizada, cantidadEstudiantes, justificacionNormalizada);
    }
}
