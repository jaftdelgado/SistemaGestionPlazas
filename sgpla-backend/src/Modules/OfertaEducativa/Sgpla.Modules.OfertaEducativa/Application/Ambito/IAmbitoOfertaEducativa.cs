namespace Sgpla.Modules.OfertaEducativa.Application.Ambito;

/// <summary>Ámbito del usuario en curso sobre la oferta educativa (DATABASE.md §13.1; Modulo_OfertaEducativa.md §6).</summary>
internal interface IAmbitoOfertaEducativa
{
    /// <summary>
    /// Ids de las entidades académicas que el usuario puede consultar, incluidas las dadas de baja; <c>null</c> significa
    /// todas (Superusuario).
    /// </summary>
    Task<IReadOnlyCollection<int>?> EntidadesVisiblesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// <c>true</c> si el usuario es DGAA y la entidad está activa y pertenece a su área. Para cualquier otro rol,
    /// <c>false</c>.
    /// </summary>
    Task<bool> PuedeEscribirEnEntidadAsync(int entidadAcademicaId, CancellationToken cancellationToken);
}
