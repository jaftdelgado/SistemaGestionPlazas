using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.SolicitudesApertura.Application.Ambito;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.SolicitudesApertura.Infrastructure.Ambito;

/// <summary>Falla cerrada: un rol distinto, una cuenta sin su entidad o su área, o una EE inexistente no ven nada.</summary>
internal sealed class AmbitoSolicitudesApertura(ICurrentUser actual, IExperienciasEducativas experiencias)
    : IAmbitoSolicitudesApertura
{
    public async Task<ExperienciaEducativaResumen?> ExperienciaDeSuEntidadAsync(
        int experienciaEducativaId,
        CancellationToken cancellationToken)
    {
        if (actual.Rol != Rol.EntidadAcademica || actual.EntidadAcademicaId is not { } entidadAcademicaId)
        {
            return null;
        }

        var resumenes = await experiencias.ObtenerAsync([experienciaEducativaId], cancellationToken);
        return resumenes.TryGetValue(experienciaEducativaId, out var resumen) && resumen.EntidadAcademicaId == entidadAcademicaId
            ? resumen
            : null;
    }

    public async Task<ExperienciaEducativaResumen?> ExperienciaDeSuAreaAsync(
        int experienciaEducativaId,
        CancellationToken cancellationToken)
    {
        if (actual.Rol != Rol.Dgaa || actual.AreaAcademicaId is not { } areaAcademicaId)
        {
            return null;
        }

        var resumenes = await experiencias.ObtenerAsync([experienciaEducativaId], cancellationToken);
        return resumenes.TryGetValue(experienciaEducativaId, out var resumen) && resumen.AreaAcademicaId == areaAcademicaId
            ? resumen
            : null;
    }
}
