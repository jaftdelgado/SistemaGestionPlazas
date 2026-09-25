using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Catalogos.Application.GradosAcademicos;
using Sgpla.Modules.Catalogos.Domain.GradosAcademicos;

namespace Sgpla.Modules.Catalogos.Infrastructure.GradosAcademicos;

internal sealed class GradoAcademicoQueries(SgplaDbContext contexto) : IGradoAcademicoQueries
{
    public Task<GradoAcademicoResponse?> ObtenerAsync(int id, CancellationToken cancellationToken) =>
        contexto.Set<GradoAcademico>()
            .AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => new GradoAcademicoResponse(g.Id, g.Nombre))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<GradoAcademicoResponse>> ListarAsync(CancellationToken cancellationToken) =>
        await contexto.Set<GradoAcademico>()
            .AsNoTracking()
            .OrderBy(g => g.Id)
            .Select(g => new GradoAcademicoResponse(g.Id, g.Nombre))
            .ToListAsync(cancellationToken);
}
