using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Application.Ambito;
using Sgpla.Modules.OfertaEducativa.Application.PlanesEstudio;
using Sgpla.Modules.OfertaEducativa.Application.ProgramasEducativos;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;
using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;
using Sgpla.Modules.OfertaEducativa.Infrastructure.ProgramasEducativos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.PlanesEstudio;

internal sealed class ListarPlanesEstudioHandler(
    SgplaDbContext contexto,
    IAmbitoOfertaEducativa ambito,
    IAmbitosInstitucionales ambitosInstitucionales)
    : IQueryHandler<ListarPlanesEstudioQuery, Pagina<PlanEstudiosResponse>>
{
    public async Task<Result<Pagina<PlanEstudiosResponse>>> HandleAsync(
        ListarPlanesEstudioQuery query,
        CancellationToken cancellationToken)
    {
        var filtros = query.Filtros;

        // El ámbito va antes que los demás filtros.
        var programas = await ProgramaEducativoAmbito.AplicarAsync(
            contexto.Set<ProgramaEducativo>().AsNoTracking(), ambito, cancellationToken);

        var planes = PlanEstudiosIntermedio.PlanesConPrograma(contexto, programas);

        if (filtros.ProgramaEducativoId is not null)
        {
            planes = planes.Where(x => x.Programa.Id == filtros.ProgramaEducativoId);
        }

        if (filtros.EntidadAcademicaId is not null)
        {
            planes = planes.Where(x => x.Programa.EntidadAcademicaId == filtros.EntidadAcademicaId);
        }

        // El código se guarda en mayúsculas: se busca en mayúsculas para no depender de la colación del servidor.
        var busqueda = Normalizacion.Recortar(filtros.Busqueda).ToUpperInvariant();
        if (busqueda.Length > 0)
        {
            planes = planes.Where(x => x.Plan.Codigo.Contains(busqueda));
        }

        var pagina = await PlanEstudiosIntermedio
            .Proyectar(contexto, planes.OrderBy(x => x.Plan.Codigo).ThenBy(x => x.Plan.Id))
            .PaginarAsync(query.Paginacion, cancellationToken);

        var elementos = await PlanEstudiosIntermedio.ArmarRespuestasAsync(
            pagina.Elementos, ambitosInstitucionales, cancellationToken);

        return new Pagina<PlanEstudiosResponse>(elementos, pagina.NumeroPagina, pagina.TamanoPagina, pagina.Total);
    }
}

internal sealed class ObtenerPlanEstudiosHandler(
    SgplaDbContext contexto,
    IAmbitoOfertaEducativa ambito,
    IAmbitosInstitucionales ambitosInstitucionales)
    : IQueryHandler<ObtenerPlanEstudiosQuery, PlanEstudiosResponse>
{
    public async Task<Result<PlanEstudiosResponse>> HandleAsync(
        ObtenerPlanEstudiosQuery query,
        CancellationToken cancellationToken)
    {
        var programas = await ProgramaEducativoAmbito.AplicarAsync(
            contexto.Set<ProgramaEducativo>().AsNoTracking(), ambito, cancellationToken);

        var planes = PlanEstudiosIntermedio.PlanesConPrograma(contexto, programas).Where(x => x.Plan.Id == query.Id);
        var encontrado = await PlanEstudiosIntermedio.Proyectar(contexto, planes).FirstOrDefaultAsync(cancellationToken);

        if (encontrado is null)
        {
            return PlanEstudiosErrors.NoEncontrado(query.Id);
        }

        var respuestas = await PlanEstudiosIntermedio.ArmarRespuestasAsync(
            [encontrado], ambitosInstitucionales, cancellationToken);
        return respuestas[0];
    }
}

/// <summary>Plan junto con su programa, ya restringido al ámbito del usuario.</summary>
internal sealed class PlanConPrograma
{
    public required PlanEstudios Plan { get; init; }

    public required ProgramaEducativo Programa { get; init; }
}

/// <summary>Plan con solo los ids; el nombre de la entidad se resuelve con una llamada por contrato, sobre toda la página.</summary>
internal sealed record PlanEstudiosIntermedio(
    int Id,
    string Codigo,
    int ProgramaEducativoId,
    string ProgramaEducativoNombre,
    int EntidadAcademicaId,
    int ExperienciasEducativas)
{
    public static IQueryable<PlanConPrograma> PlanesConPrograma(SgplaDbContext contexto, IQueryable<ProgramaEducativo> programas) =>
        from plan in contexto.Set<PlanEstudios>().AsNoTracking()
        join programa in programas on plan.ProgramaEducativoId equals programa.Id
        select new PlanConPrograma { Plan = plan, Programa = programa };

    /// <summary><c>ExperienciasEducativas</c> cuenta solo las activas, por el filtro global de la EE.</summary>
    public static IQueryable<PlanEstudiosIntermedio> Proyectar(SgplaDbContext contexto, IQueryable<PlanConPrograma> planes) =>
        planes.Select(x => new PlanEstudiosIntermedio(
            x.Plan.Id,
            x.Plan.Codigo,
            x.Programa.Id,
            x.Programa.Nombre,
            x.Programa.EntidadAcademicaId,
            contexto.Set<ExperienciaEducativa>().Count(e => e.PlanEstudiosId == x.Plan.Id)));

    public static async Task<List<PlanEstudiosResponse>> ArmarRespuestasAsync(
        IReadOnlyList<PlanEstudiosIntermedio> planes,
        IAmbitosInstitucionales ambitosInstitucionales,
        CancellationToken cancellationToken)
    {
        var entidades = await ambitosInstitucionales.ObtenerEntidadesAsync(
            planes.Select(p => p.EntidadAcademicaId).Distinct().ToList(), cancellationToken);

        return planes.Select(p =>
        {
            var entidad = entidades[p.EntidadAcademicaId];

            return new PlanEstudiosResponse(
                p.Id,
                p.Codigo,
                new ProgramaEducativoResumenResponse(p.ProgramaEducativoId, p.ProgramaEducativoNombre),
                new EntidadAcademicaResumenResponse(entidad.Id, entidad.Clave, entidad.Nombre),
                p.ExperienciasEducativas);
        }).ToList();
    }
}
