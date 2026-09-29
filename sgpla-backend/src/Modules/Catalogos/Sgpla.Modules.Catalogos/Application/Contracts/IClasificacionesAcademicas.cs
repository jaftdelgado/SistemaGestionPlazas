namespace Sgpla.Modules.Catalogos.Application.Contracts;

/// <summary>
/// Consulta de sistemas educativos, niveles y áreas de formación para otros módulos (OfertaEducativa). Un id existe si
/// aparece en el diccionario; los inexistentes no aparecen. Con una colección vacía no se consulta la base.
/// </summary>
public interface IClasificacionesAcademicas
{
    Task<IReadOnlyDictionary<int, SistemaEducativoResumen>> ObtenerSistemasEducativosAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<int, NivelFormacionResumen>> ObtenerNivelesFormacionAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<int, AreaFormacionResumen>> ObtenerAreasFormacionAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken);
}

public sealed record SistemaEducativoResumen(int Id, string Nombre);

public sealed record NivelFormacionResumen(int Id, string Clave, string Nombre);

public sealed record AreaFormacionResumen(int Id, string Clave, string Nombre);
