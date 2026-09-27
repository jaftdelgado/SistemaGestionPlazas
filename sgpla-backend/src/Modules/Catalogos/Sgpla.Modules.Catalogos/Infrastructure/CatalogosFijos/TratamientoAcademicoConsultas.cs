using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Catalogos.Application.CatalogosFijos;
using Sgpla.Modules.Catalogos.Domain.CatalogosFijos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Infrastructure.CatalogosFijos;

internal sealed class ListarTratamientosAcademicosHandler(SgplaDbContext contexto)
    : IQueryHandler<ListarTratamientosAcademicosQuery, IReadOnlyList<TratamientoAcademicoResponse>>
{
    public async Task<Result<IReadOnlyList<TratamientoAcademicoResponse>>> HandleAsync(
        ListarTratamientosAcademicosQuery query,
        CancellationToken cancellationToken)
    {
        var tratamientos = contexto.Set<TratamientoAcademico>().AsQueryable();
        if (query.GradoAcademicoId is not null)
        {
            tratamientos = tratamientos.Where(t => t.GradoAcademicoId == query.GradoAcademicoId);
        }

        return Result.Success<IReadOnlyList<TratamientoAcademicoResponse>>(
            await TratamientoAcademicoProyeccion.ConGrado(contexto, tratamientos).ToListAsync(cancellationToken));
    }
}

internal sealed class ObtenerTratamientoAcademicoHandler(SgplaDbContext contexto)
    : IQueryHandler<ObtenerTratamientoAcademicoQuery, TratamientoAcademicoResponse>
{
    public async Task<Result<TratamientoAcademicoResponse>> HandleAsync(
        ObtenerTratamientoAcademicoQuery query,
        CancellationToken cancellationToken)
    {
        var encontrado = await TratamientoAcademicoProyeccion
            .ConGrado(contexto, contexto.Set<TratamientoAcademico>().Where(t => t.Id == query.Id))
            .FirstOrDefaultAsync(cancellationToken);

        return encontrado is null ? CatalogoFijoErrors.NoEncontrado<TratamientoAcademico>(query.Id) : encontrado;
    }
}

internal static class TratamientoAcademicoProyeccion
{
    /// <summary>Une cada tratamiento con su grado, en orden de id del tratamiento.</summary>
    public static IQueryable<TratamientoAcademicoResponse> ConGrado(
        SgplaDbContext contexto,
        IQueryable<TratamientoAcademico> tratamientos) =>
        from t in tratamientos.AsNoTracking()
        join g in contexto.Set<GradoAcademico>().AsNoTracking() on t.GradoAcademicoId equals g.Id
        orderby t.Id
        select new TratamientoAcademicoResponse(t.Id, t.Nombre, new CatalogoFijoResponse(g.Id, g.Nombre));
}
