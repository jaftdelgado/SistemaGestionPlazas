using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;

namespace Sgpla.Modules.SolicitudesApertura.Infrastructure.Contratos;

internal sealed class ReferenciasExperienciaEducativaEnSolicitudes(SgplaDbContext contexto) : IReferenciasExperienciaEducativa
{
    public Task<bool> TieneReferenciasAsync(
        IReadOnlyCollection<int> experienciaEducativaIds,
        CancellationToken cancellationToken) =>
        experienciaEducativaIds.Count == 0
            ? Task.FromResult(false)
            : contexto.Set<SolicitudApertura>().AnyAsync(
                s => experienciaEducativaIds.Contains(s.ExperienciaEducativaId)
                    && (s.Estado == EstadoSolicitudApertura.Pendiente || s.Estado == EstadoSolicitudApertura.Aceptada),
                cancellationToken);
}
