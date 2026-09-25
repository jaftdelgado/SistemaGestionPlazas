using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Catalogos.Domain.GradosAcademicos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Application.GradosAcademicos.Obtener;

internal sealed class ObtenerGradoAcademicoHandler(IGradoAcademicoQueries consultas)
    : IQueryHandler<ObtenerGradoAcademicoQuery, GradoAcademicoResponse>
{
    public async Task<Result<GradoAcademicoResponse>> HandleAsync(
        ObtenerGradoAcademicoQuery query,
        CancellationToken cancellationToken)
    {
        var gradoAcademico = await consultas.ObtenerAsync(query.Id, cancellationToken);

        return gradoAcademico is null ? GradoAcademicoErrors.NoEncontrado(query.Id) : gradoAcademico;
    }
}
