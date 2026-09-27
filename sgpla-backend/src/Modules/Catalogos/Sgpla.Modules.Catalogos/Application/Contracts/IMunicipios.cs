namespace Sgpla.Modules.Catalogos.Application.Contracts;

/// <summary>Consulta de municipios para otros módulos (Institucional, por el domicilio de una entidad académica).</summary>
public interface IMunicipios
{
    Task<bool> ExisteAsync(int id, CancellationToken cancellationToken);

    /// <summary>Nombre de cada id que existe; los que no existen no aparecen en el diccionario.</summary>
    Task<IReadOnlyDictionary<int, string>> ObtenerNombresAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken);
}
