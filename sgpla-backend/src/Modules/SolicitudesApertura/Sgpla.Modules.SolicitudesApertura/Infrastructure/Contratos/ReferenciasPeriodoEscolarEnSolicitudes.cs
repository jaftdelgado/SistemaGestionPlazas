using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;

namespace Sgpla.Modules.SolicitudesApertura.Infrastructure.Contratos;

internal sealed class ReferenciasPeriodoEscolarEnSolicitudes(SgplaDbContext contexto) : IReferenciasPeriodoEscolar
{
    public Task<bool> TieneReferenciasAsync(int periodoEscolarId, CancellationToken cancellationToken) =>
        contexto.Set<SolicitudApertura>().AnyAsync(s => s.PeriodoEscolarId == periodoEscolarId, cancellationToken);
}
