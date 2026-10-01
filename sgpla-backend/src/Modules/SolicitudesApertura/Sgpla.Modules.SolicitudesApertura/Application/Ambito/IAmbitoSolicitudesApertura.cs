using Sgpla.Modules.OfertaEducativa.Application.Contracts;

namespace Sgpla.Modules.SolicitudesApertura.Application.Ambito;

/// <summary>Ámbito del usuario en curso sobre las solicitudes, a partir de la experiencia educativa que solicitan.</summary>
internal interface IAmbitoSolicitudesApertura
{
    /// <summary>Resumen de la EE si el usuario es Entidad Académica y la EE es de su entidad; si no, <c>null</c>.</summary>
    Task<ExperienciaEducativaResumen?> ExperienciaDeSuEntidadAsync(int experienciaEducativaId, CancellationToken cancellationToken);

    /// <summary>Resumen de la EE si el usuario es DGAA y la EE es de una entidad de su área; si no, <c>null</c>.</summary>
    Task<ExperienciaEducativaResumen?> ExperienciaDeSuAreaAsync(int experienciaEducativaId, CancellationToken cancellationToken);
}
