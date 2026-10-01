using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;

namespace Sgpla.Modules.SolicitudesApertura.Infrastructure.SolicitudesApertura;

internal sealed class SolicitudAperturaRepository(SgplaDbContext contexto) : ISolicitudAperturaRepository
{
    public Task<SolicitudApertura?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        contexto.Set<SolicitudApertura>()
            .Include(s => s.Oficio)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> ExistePendienteAsync(
        int experienciaEducativaId,
        int periodoEscolarId,
        string seccion,
        CancellationToken cancellationToken) =>
        contexto.Set<SolicitudApertura>().AnyAsync(
            s => s.ExperienciaEducativaId == experienciaEducativaId
                && s.PeriodoEscolarId == periodoEscolarId
                && s.Seccion == seccion
                && s.Estado == EstadoSolicitudApertura.Pendiente,
            cancellationToken);

    public void Agregar(SolicitudApertura solicitud) => contexto.Set<SolicitudApertura>().Add(solicitud);

    public void EliminarOficio(ArchivoSolicitudApertura oficio) =>
        contexto.Set<ArchivoSolicitudApertura>().Remove(oficio);
}
