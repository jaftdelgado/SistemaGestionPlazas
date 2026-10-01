namespace Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;

internal enum EstadoSolicitudApertura
{
    Pendiente,
    Aceptada,
    Rechazada,
    Cancelada,
}

internal static class EstadoSolicitudAperturaTexto
{
    /// <summary>Texto que guarda la base y que devuelve la API: PENDIENTE, ACEPTADA, RECHAZADA o CANCELADA.</summary>
    public static string ComoTexto(this EstadoSolicitudApertura estado) => estado.ToString().ToUpperInvariant();
}
