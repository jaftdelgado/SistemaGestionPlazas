using Sgpla.BuildingBlocks.Application;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Application.GradosAcademicos.Listar;

internal sealed class ListarGradosAcademicosHandler(IGradoAcademicoQueries consultas)
    : IQueryHandler<ListarGradosAcademicosQuery, IReadOnlyList<GradoAcademicoResponse>>
{
    public async Task<Result<IReadOnlyList<GradoAcademicoResponse>>> HandleAsync(
        ListarGradosAcademicosQuery query,
        CancellationToken cancellationToken) =>
        Result.Success(await consultas.ListarAsync(cancellationToken));
}
