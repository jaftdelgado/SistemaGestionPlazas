namespace Sgpla.Modules.Catalogos.Application.GradosAcademicos;

internal interface IGradoAcademicoQueries
{
    Task<GradoAcademicoResponse?> ObtenerAsync(int id, CancellationToken cancellationToken);

    /// <summary>Todos los grados, en orden de id (jerarquía académica).</summary>
    Task<IReadOnlyList<GradoAcademicoResponse>> ListarAsync(CancellationToken cancellationToken);
}
