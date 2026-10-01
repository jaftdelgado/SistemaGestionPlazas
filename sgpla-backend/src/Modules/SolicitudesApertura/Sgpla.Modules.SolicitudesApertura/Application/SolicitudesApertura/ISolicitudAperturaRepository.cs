using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;

namespace Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;

internal interface ISolicitudAperturaRepository
{
    /// <summary>Con su oficio cargado.</summary>
    Task<SolicitudApertura?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Solo solicitudes PENDIENTE; la sección llega ya normalizada.</summary>
    Task<bool> ExistePendienteAsync(
        int experienciaEducativaId,
        int periodoEscolarId,
        string seccion,
        CancellationToken cancellationToken);

    void Agregar(SolicitudApertura solicitud);

    /// <summary>Borrado físico de un oficio reemplazado.</summary>
    void EliminarOficio(ArchivoSolicitudApertura oficio);
}
