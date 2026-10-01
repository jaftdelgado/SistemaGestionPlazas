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

        var cantidad = SolicitudApertura.ValidarCantidad(cantidadEstudiantes);
        if (cantidad.IsFailure)
        {
            return cantidad.Error;
        }

        var justificacionNormalizada = SolicitudApertura.NormalizarJustificacion(justificacion);
        if (justificacionNormalizada.IsFailure)
        {
            return justificacionNormalizada.Error;
        }

        return new DatosSolicitudApertura(seccionNormalizada, cantidadEstudiantes, justificacionNormalizada.Value);
    }
}
