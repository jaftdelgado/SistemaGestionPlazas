using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Application.Ambito;
using Sgpla.Modules.OfertaEducativa.Application.ExperienciasEducativas;
using Sgpla.Modules.OfertaEducativa.Application.PlanesEstudio;
using Sgpla.Modules.OfertaEducativa.Application.Programaciones;
using Sgpla.Modules.OfertaEducativa.Application.ProgramasEducativos;
using Sgpla.Modules.OfertaEducativa.Domain.PeriodosEscolares;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;
using Sgpla.Modules.OfertaEducativa.Domain.Programaciones;
using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;
using Sgpla.Modules.OfertaEducativa.Infrastructure.ProgramasEducativos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.Programaciones;

internal sealed class ListarProgramacionesAcademicasHandler(
    SgplaDbContext contexto,
    IAmbitoOfertaEducativa ambito,
    IAmbitosInstitucionales ambitosInstitucionales)
    : IQueryHandler<ListarProgramacionesAcademicasQuery, Pagina<ProgramacionAcademicaResponse>>
{
    public async Task<Result<Pagina<ProgramacionAcademicaResponse>>> HandleAsync(
        ListarProgramacionesAcademicasQuery query,
        CancellationToken cancellationToken)
    {
        var filtros = query.Filtros;

        // El ámbito va antes que los demás filtros.
        var programas = await ProgramaEducativoAmbito.AplicarAsync(
            contexto.Set<ProgramaEducativo>().AsNoTracking(), ambito, cancellationToken);

        var programaciones = ProgramacionAcademicaIntermedia.ProgramacionesConContexto(contexto, programas);

        if (filtros.PeriodoEscolarId is not null)
        {
            programaciones = programaciones.Where(x => x.Periodo.Id == filtros.PeriodoEscolarId);
        }

        if (filtros.EntidadAcademicaId is not null)
        {
            programaciones = programaciones.Where(x => x.Programa.EntidadAcademicaId == filtros.EntidadAcademicaId);
        }

        if (filtros.ProgramaEducativoId is not null)
        {
            programaciones = programaciones.Where(x => x.Programa.Id == filtros.ProgramaEducativoId);
        }

        if (filtros.PlanEstudiosId is not null)
        {
            programaciones = programaciones.Where(x => x.Plan.Id == filtros.PlanEstudiosId);
        }

        if (filtros.ExperienciaEducativaId is not null)
        {
            programaciones = programaciones.Where(x => x.Experiencia.Id == filtros.ExperienciaEducativaId);
        }

        // El NRC se guarda en mayúsculas: se compara exacto, en mayúsculas.
        var nrc = Normalizacion.Recortar(filtros.Nrc).ToUpperInvariant();
        if (nrc.Length > 0)
        {
            programaciones = programaciones.Where(x => x.Programacion.Nrc == nrc);
        }

        var pagina = await ProgramacionAcademicaIntermedia
            .Proyectar(programaciones
                .OrderByDescending(x => x.Periodo.Clave)
                .ThenBy(x => x.Programacion.Nrc)
                .ThenBy(x => x.Programacion.Id))
            .PaginarAsync(query.Paginacion, cancellationToken);

        var elementos = await ProgramacionAcademicaIntermedia.ArmarRespuestasAsync(
            pagina.Elementos, ambitosInstitucionales, cancellationToken);

        return new Pagina<ProgramacionAcademicaResponse>(elementos, pagina.NumeroPagina, pagina.TamanoPagina, pagina.Total);
    }
}

internal sealed class ObtenerProgramacionAcademicaHandler(
    SgplaDbContext contexto,
    IAmbitoOfertaEducativa ambito,
    IAmbitosInstitucionales ambitosInstitucionales)
    : IQueryHandler<ObtenerProgramacionAcademicaQuery, ProgramacionAcademicaResponse>
{
    public async Task<Result<ProgramacionAcademicaResponse>> HandleAsync(
        ObtenerProgramacionAcademicaQuery query,
        CancellationToken cancellationToken)
    {
        var programas = await ProgramaEducativoAmbito.AplicarAsync(
            contexto.Set<ProgramaEducativo>().AsNoTracking(), ambito, cancellationToken);

        var programaciones = ProgramacionAcademicaIntermedia
            .ProgramacionesConContexto(contexto, programas)
            .Where(x => x.Programacion.Id == query.Id);
        var encontrada = await ProgramacionAcademicaIntermedia.Proyectar(programaciones).FirstOrDefaultAsync(cancellationToken);

        if (encontrada is null)
        {
            return ProgramacionAcademicaErrors.NoEncontrado(query.Id);
        }

        var respuestas = await ProgramacionAcademicaIntermedia.ArmarRespuestasAsync(
            [encontrada], ambitosInstitucionales, cancellationToken);
        return respuestas[0];
    }
}

internal sealed class ListarHorariosHandler(SgplaDbContext contexto, IAmbitoOfertaEducativa ambito)
    : IQueryHandler<ListarHorariosQuery, IReadOnlyList<HorarioResponse>>
{
    public async Task<Result<IReadOnlyList<HorarioResponse>>> HandleAsync(
        ListarHorariosQuery query,
        CancellationToken cancellationToken)
    {
        var programas = await ProgramaEducativoAmbito.AplicarAsync(
            contexto.Set<ProgramaEducativo>().AsNoTracking(), ambito, cancellationToken);

        // La programación debe estar a la vista, igual que al obtenerla.
        var visible = await ProgramacionAcademicaIntermedia
            .ProgramacionesConContexto(contexto, programas)
            .AnyAsync(x => x.Programacion.Id == query.ProgramacionAcademicaId, cancellationToken);

        if (!visible)
        {
            return ProgramacionAcademicaErrors.NoEncontrado(query.ProgramacionAcademicaId);
        }

        // Todas las filas: la tabla solo guarda el snapshot vigente de PLANEA (DATABASE.md §5).
        var horarios = await contexto.Set<HorarioProgramacion>()
            .AsNoTracking()
            .Where(h => h.ProgramacionAcademicaId == query.ProgramacionAcademicaId)
            .OrderBy(h => h.DiaSemana)
            .ThenBy(h => h.HoraInicio)
            .ThenBy(h => h.Id)
            .Select(h => new HorarioResponse(
                h.Id, h.DiaSemana, h.HoraInicio, h.HoraFin, h.FechaInicio, h.FechaFin, h.Edificio, h.Aula))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<HorarioResponse>>(horarios);
    }
}

/// <summary>Programación junto con su periodo, EE, plan y programa, ya restringida al ámbito del usuario.</summary>
internal sealed class ProgramacionConContexto
{
    public required ProgramacionAcademica Programacion { get; init; }

    public required PeriodoEscolar Periodo { get; init; }

    public required ExperienciaEducativa Experiencia { get; init; }

    public required PlanEstudios Plan { get; init; }

    public required ProgramaEducativo Programa { get; init; }
}

/// <summary>Programación con solo los ids; el nombre de la entidad se resuelve con una llamada por contrato, sobre toda la página.</summary>
internal sealed record ProgramacionAcademicaIntermedia(
    int Id,
    string Nrc,
    int PeriodoEscolarId,
    string PeriodoEscolarClave,
    int ExperienciaEducativaId,
    string Materia,
    string Curso,
    string ExperienciaEducativaNombre,
    int PlanEstudiosId,
    string PlanEstudiosCodigo,
    int ProgramaEducativoId,
    string ProgramaEducativoNombre,
    int EntidadAcademicaId)
{
    /// <summary>Periodo, EE, plan y programa son activos por el filtro global de cada entidad.</summary>
    public static IQueryable<ProgramacionConContexto> ProgramacionesConContexto(
        SgplaDbContext contexto, IQueryable<ProgramaEducativo> programas) =>
        from programacion in contexto.Set<ProgramacionAcademica>().AsNoTracking()
        join periodo in contexto.Set<PeriodoEscolar>().AsNoTracking() on programacion.PeriodoEscolarId equals periodo.Id
        join experiencia in contexto.Set<ExperienciaEducativa>().AsNoTracking()
            on programacion.ExperienciaEducativaId equals experiencia.Id
        join plan in contexto.Set<PlanEstudios>().AsNoTracking() on experiencia.PlanEstudiosId equals plan.Id
        join programa in programas on plan.ProgramaEducativoId equals programa.Id
        select new ProgramacionConContexto
        {
            Programacion = programacion,
            Periodo = periodo,
            Experiencia = experiencia,
            Plan = plan,
            Programa = programa,
        };

    public static IQueryable<ProgramacionAcademicaIntermedia> Proyectar(IQueryable<ProgramacionConContexto> programaciones) =>
        programaciones.Select(x => new ProgramacionAcademicaIntermedia(
            x.Programacion.Id,
            x.Programacion.Nrc,
            x.Periodo.Id,
            x.Periodo.Clave,
            x.Experiencia.Id,
            x.Experiencia.Materia,
            x.Experiencia.Curso,
            x.Experiencia.Nombre,
            x.Plan.Id,
            x.Plan.Codigo,
            x.Programa.Id,
            x.Programa.Nombre,
            x.Programa.EntidadAcademicaId));

    public static async Task<List<ProgramacionAcademicaResponse>> ArmarRespuestasAsync(
        IReadOnlyList<ProgramacionAcademicaIntermedia> programaciones,
        IAmbitosInstitucionales ambitosInstitucionales,
        CancellationToken cancellationToken)
    {
        var entidades = await ambitosInstitucionales.ObtenerEntidadesAsync(
            programaciones.Select(p => p.EntidadAcademicaId).Distinct().ToList(), cancellationToken);

        return programaciones.Select(p =>
        {
            var entidad = entidades[p.EntidadAcademicaId];

            return new ProgramacionAcademicaResponse(
                p.Id,
                p.Nrc,
                new PeriodoEscolarResumenResponse(p.PeriodoEscolarId, p.PeriodoEscolarClave),
                new ExperienciaEducativaResumenResponse(p.ExperienciaEducativaId, p.Materia, p.Curso, p.ExperienciaEducativaNombre),
                new PlanEstudiosResumenResponse(p.PlanEstudiosId, p.PlanEstudiosCodigo),
                new ProgramaEducativoResumenResponse(p.ProgramaEducativoId, p.ProgramaEducativoNombre),
                new EntidadAcademicaResumenResponse(entidad.Id, entidad.Clave, entidad.Nombre));
        }).ToList();
    }
}
