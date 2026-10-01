namespace Sgpla.Modules.OfertaEducativa.Application.Contracts;

/// <summary>Consulta de experiencias educativas para otros módulos (SolicitudesApertura).</summary>
public interface IExperienciasEducativas
{
    /// <summary>
    /// Resumen de cada id que existe, incluidas las EE dadas de baja o con su plan o su programa dados de baja. Los
    /// inexistentes no aparecen. Con una colección vacía no se consulta la base.
    /// </summary>
    Task<IReadOnlyDictionary<int, ExperienciaEducativaResumen>> ObtenerAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken);

    /// <summary>
    /// Ids de las EE que el usuario en curso puede consultar, incluidas las dadas de baja, como consulta componible
    /// sobre la misma base: se ejecuta dentro de la consulta de quien la usa. Con <paramref name="entidadAcademicaId"/>,
    /// solo las de esa entidad, y ninguna si está fuera del ámbito. Desactiva los filtros de baja lógica de sus tablas.
    /// </summary>
    Task<IQueryable<int>> ConsultarIdsVisiblesAsync(int? entidadAcademicaId, CancellationToken cancellationToken);
}

/// <param name="Vigente"><c>true</c> si la EE, su plan y su programa están activos.</param>
public sealed record ExperienciaEducativaResumen(
    int Id,
    string Materia,
    string Curso,
    string Nombre,
    int? CupoMinimo,
    int? CupoMaximo,
    bool Vigente,
    int PlanEstudiosId,
    string PlanEstudiosCodigo,
    int ProgramaEducativoId,
    string ProgramaEducativoNombre,
    int SistemaEducativoId,
    string SistemaEducativoNombre,
    int EntidadAcademicaId,
    string EntidadAcademicaClave,
    string EntidadAcademicaNombre,
    int AreaAcademicaId);
