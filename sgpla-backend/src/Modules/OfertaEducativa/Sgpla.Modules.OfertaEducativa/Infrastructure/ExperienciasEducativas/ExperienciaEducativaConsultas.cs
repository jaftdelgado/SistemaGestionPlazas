using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Application.Ambito;
using Sgpla.Modules.OfertaEducativa.Application.ExperienciasEducativas;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;
using Sgpla.Modules.OfertaEducativa.Domain.Programaciones;
using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;
using Sgpla.Modules.OfertaEducativa.Infrastructure.ProgramasEducativos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.ExperienciasEducativas;

internal sealed class ListarExperienciasEducativasDePlanHandler(
    SgplaDbContext contexto,
    IAmbitoOfertaEducativa ambito,
    IClasificacionesAcademicas clasificaciones)
    : IQueryHandler<ListarExperienciasEducativasDePlanQuery, IReadOnlyList<ExperienciaEducativaResponse>>
{
    public async Task<Result<IReadOnlyList<ExperienciaEducativaResponse>>> HandleAsync(
        ListarExperienciasEducativasDePlanQuery query,
        CancellationToken cancellationToken)
    {
        var programas = await ProgramaEducativoAmbito.AplicarAsync(
            contexto.Set<ProgramaEducativo>().AsNoTracking(), ambito, cancellationToken);

        var plan = await (
            from p in contexto.Set<PlanEstudios>().AsNoTracking()
            join programa in programas on p.ProgramaEducativoId equals programa.Id
            where p.Id == query.PlanEstudiosId
            select new PlanEstudiosResumenResponse(p.Id, p.Codigo))
            .FirstOrDefaultAsync(cancellationToken);

        if (plan is null)
        {
            return PlanEstudiosErrors.NoEncontrado(query.PlanEstudiosId);
        }

        var experiencias = await ExperienciaEducativaIntermedia
            .Proyectar(
                contexto.Set<ExperienciaEducativa>()
                    .AsNoTracking()
                    .Where(e => e.PlanEstudiosId == plan.Id)
                    .OrderBy(e => e.Materia)
                    .ThenBy(e => e.Curso)
                    .ThenBy(e => e.Id))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ExperienciaEducativaResponse>>(
            await ExperienciaEducativaIntermedia.ArmarRespuestasAsync(
                experiencias, plan, contexto, clasificaciones, cancellationToken));
    }
}

internal sealed class ObtenerExperienciaEducativaHandler(
    SgplaDbContext contexto,
    IAmbitoOfertaEducativa ambito,
    IClasificacionesAcademicas clasificaciones)
    : IQueryHandler<ObtenerExperienciaEducativaQuery, ExperienciaEducativaResponse>
{
    public async Task<Result<ExperienciaEducativaResponse>> HandleAsync(
        ObtenerExperienciaEducativaQuery query,
        CancellationToken cancellationToken)
    {
        var programas = await ProgramaEducativoAmbito.AplicarAsync(
            contexto.Set<ProgramaEducativo>().AsNoTracking(), ambito, cancellationToken);

        var plan = await (
            from e in contexto.Set<ExperienciaEducativa>().AsNoTracking()
            join p in contexto.Set<PlanEstudios>().AsNoTracking() on e.PlanEstudiosId equals p.Id
            join programa in programas on p.ProgramaEducativoId equals programa.Id
            where e.Id == query.Id
            select new PlanEstudiosResumenResponse(p.Id, p.Codigo))
            .FirstOrDefaultAsync(cancellationToken);

        if (plan is null)
        {
            return ExperienciaEducativaErrors.NoEncontrado(query.Id);
        }

        var experiencia = await ExperienciaEducativaIntermedia
            .Proyectar(contexto.Set<ExperienciaEducativa>().AsNoTracking().Where(e => e.Id == query.Id))
            .FirstAsync(cancellationToken);

        var respuestas = await ExperienciaEducativaIntermedia.ArmarRespuestasAsync(
            [experiencia], plan, contexto, clasificaciones, cancellationToken);
        return respuestas[0];
    }
}

/// <summary>EE con solo el id del área; el nombre del área se resuelve con una llamada por contrato, sobre toda la lista.</summary>
internal sealed record ExperienciaEducativaIntermedia(
    int Id,
    string Nombre,
    string Materia,
    string Curso,
    int HorasTeoricas,
    int HorasPracticas,
    int Creditos,
    int? CupoMinimo,
    int? CupoMaximo,
    string? PerfilDocente,
    int AreaFormacionId)
{
    public static IQueryable<ExperienciaEducativaIntermedia> Proyectar(IQueryable<ExperienciaEducativa> experiencias) =>
        experiencias.Select(e => new ExperienciaEducativaIntermedia(
            e.Id,
            e.Nombre,
            e.Materia,
            e.Curso,
            e.HorasTeoricas,
            e.HorasPracticas,
            e.Creditos,
            e.CupoMinimo,
            e.CupoMaximo,
            e.PerfilDocente,
            e.AreaFormacionId));

    public static async Task<List<ExperienciaEducativaResponse>> ArmarRespuestasAsync(
        IReadOnlyList<ExperienciaEducativaIntermedia> experiencias,
        PlanEstudiosResumenResponse plan,
        SgplaDbContext contexto,
        IClasificacionesAcademicas clasificaciones,
        CancellationToken cancellationToken)
    {
        if (experiencias.Count == 0)
        {
            return [];
        }

        var areas = await clasificaciones.ObtenerAreasFormacionAsync(
            experiencias.Select(e => e.AreaFormacionId).Distinct().ToList(), cancellationToken);
        var programadas = await ConProgramacionesAsync(contexto, experiencias.Select(e => e.Id).ToList(), cancellationToken);

        return experiencias.Select(e =>
        {
            var area = areas[e.AreaFormacionId];

            return new ExperienciaEducativaResponse(
                e.Id,
                e.Nombre,
                e.Materia,
                e.Curso,
                e.HorasTeoricas,
                e.HorasPracticas,
                e.Creditos,
                e.CupoMinimo,
                e.CupoMaximo,
                e.PerfilDocente,
                new AreaFormacionResponse(area.Id, area.Clave, area.Nombre),
                plan,
                programadas.Contains(e.Id));
        }).ToList();
    }

    /// <summary>
    /// Ids de las EE con cualquier programación, incluidas las dadas de baja (decisión D13). Va en su propia consulta:
    /// <c>IgnoreQueryFilters</c> dentro de la consulta principal se extendería también al filtro de baja de las EE.
    /// </summary>
    private static async Task<HashSet<int>> ConProgramacionesAsync(
        SgplaDbContext contexto,
        IReadOnlyCollection<int> experienciaIds,
        CancellationToken cancellationToken) =>
        (await contexto.Set<ProgramacionAcademica>()
            .IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
            .Where(p => experienciaIds.Contains(p.ExperienciaEducativaId))
            .Select(p => p.ExperienciaEducativaId)
            .Distinct()
            .ToListAsync(cancellationToken))
        .ToHashSet();
}
