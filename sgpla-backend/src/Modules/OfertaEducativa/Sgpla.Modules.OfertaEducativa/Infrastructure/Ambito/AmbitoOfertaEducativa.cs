using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Application.Ambito;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.Ambito;

/// <summary>Falla cerrada: un rol desconocido, o un DGAA o una Entidad Académica sin su ámbito, no ve ni escribe nada.</summary>
internal sealed class AmbitoOfertaEducativa(ICurrentUser actual, IAmbitosInstitucionales ambitos) : IAmbitoOfertaEducativa
{
    public async Task<IReadOnlyCollection<int>?> EntidadesVisiblesAsync(CancellationToken cancellationToken)
    {
        if (actual.Rol == Rol.Superusuario)
        {
            return null;
        }

        if (actual.Rol == Rol.Dgaa && actual.AreaAcademicaId is { } areaAcademicaId)
        {
            return await ambitos.ObtenerEntidadesDeAreaAsync(areaAcademicaId, cancellationToken);
        }

        if (actual.Rol == Rol.EntidadAcademica && actual.EntidadAcademicaId is { } entidadAcademicaId)
        {
            return [entidadAcademicaId];
        }

        return [];
    }

    public async Task<bool> PuedeEscribirEnEntidadAsync(int entidadAcademicaId, CancellationToken cancellationToken)
    {
        if (actual.Rol != Rol.Dgaa || actual.AreaAcademicaId is not { } areaAcademicaId)
        {
            return false;
        }

        if (!await ambitos.EntidadAcademicaActivaAsync(entidadAcademicaId, cancellationToken))
        {
            return false;
        }

        var entidades = await ambitos.ObtenerEntidadesAsync([entidadAcademicaId], cancellationToken);
        return entidades.TryGetValue(entidadAcademicaId, out var entidad) && entidad.AreaAcademicaId == areaAcademicaId;
    }
}
