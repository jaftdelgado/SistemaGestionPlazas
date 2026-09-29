namespace Sgpla.Modules.Institucional.Application.Contracts;

/// <summary>Consulta de áreas y entidades académicas para otros módulos (Usuarios, por el ámbito de una cuenta).</summary>
public interface IAmbitosInstitucionales
{
    Task<bool> AreaAcademicaActivaAsync(int areaAcademicaId, CancellationToken cancellationToken);

    Task<bool> EntidadAcademicaActivaAsync(int entidadAcademicaId, CancellationToken cancellationToken);

    /// <summary>Incluye las dadas de baja: una cuenta siempre puede mostrar su ámbito. Los ids inexistentes no aparecen.</summary>
    Task<IReadOnlyDictionary<int, AreaAcademicaResumen>> ObtenerAreasAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken);

    /// <summary>Igual que <see cref="ObtenerAreasAsync"/>, para entidades.</summary>
    Task<IReadOnlyDictionary<int, EntidadAcademicaResumen>> ObtenerEntidadesAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken);

    /// <summary>Ids de las entidades del área, incluidas las dadas de baja.</summary>
    Task<IReadOnlyCollection<int>> ObtenerEntidadesDeAreaAsync(int areaAcademicaId, CancellationToken cancellationToken);
}

public sealed record AreaAcademicaResumen(int Id, int Clave, string Nombre);

public sealed record EntidadAcademicaResumen(int Id, string Clave, string Nombre, int AreaAcademicaId);
