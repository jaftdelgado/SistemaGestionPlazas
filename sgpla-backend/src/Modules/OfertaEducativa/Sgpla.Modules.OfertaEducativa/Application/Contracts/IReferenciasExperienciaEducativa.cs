namespace Sgpla.Modules.OfertaEducativa.Application.Contracts;

/// <summary>
/// Lo implementan los módulos que referencian experiencias educativas (SolicitudesApertura, P7). <c>true</c> bloquea la
/// baja de esas EE y de su plan. Cada implementación decide qué referencias bloquean.
/// </summary>
public interface IReferenciasExperienciaEducativa
{
    Task<bool> TieneReferenciasAsync(
        IReadOnlyCollection<int> experienciaEducativaIds,
        CancellationToken cancellationToken);
}
