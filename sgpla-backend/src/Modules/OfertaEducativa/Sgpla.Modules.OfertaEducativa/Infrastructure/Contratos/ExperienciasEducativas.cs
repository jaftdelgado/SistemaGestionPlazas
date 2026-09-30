using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Application.Ambito;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;
using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.Contratos;

internal sealed class ExperienciasEducativas(
    SgplaDbContext contexto,
    IAmbitoOfertaEducativa ambito,
    IAmbitosInstitucionales ambitosInstitucionales,
    IClasificacionesAcademicas clasificaciones) : IExperienciasEducativas
{
    public async Task<IReadOnlyDictionary<int, ExperienciaEducativaResumen>> ObtenerAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<int, ExperienciaEducativaResumen>();
        }

        var datos = await (
            from experiencia in contexto.Set<ExperienciaEducativa>().IgnoreQueryFilters([FiltrosConsulta.BajaLogica]).AsNoTracking()
            join plan in contexto.Set<PlanEstudios>().IgnoreQueryFilters([FiltrosConsulta.BajaLogica]).AsNoTracking()
                on experiencia.PlanEstudiosId equals plan.Id
            join programa in contexto.Set<ProgramaEducativo>().IgnoreQueryFilters([FiltrosConsulta.BajaLogica]).AsNoTracking()
                on plan.ProgramaEducativoId equals programa.Id
            where ids.Contains(experiencia.Id)
            select new ExperienciaEducativaIntermedia(
                experiencia.Id,
                experiencia.Materia,
                experiencia.Curso,
                experiencia.Nombre,
                experiencia.CupoMinimo,
                experiencia.CupoMaximo,
                experiencia.FechaEliminacion == null && plan.FechaEliminacion == null && programa.FechaEliminacion == null,
                plan.Id,
                plan.Codigo,
                programa.Id,
                programa.Nombre,
                programa.SistemaEducativoId,
                programa.EntidadAcademicaId))
            .ToListAsync(cancellationToken);

        return await ArmarRespuestasAsync(datos, ambitosInstitucionales, clasificaciones, cancellationToken);
    }

    public async Task<IQueryable<int>> ConsultarIdsVisiblesAsync(
        int? entidadAcademicaId,
        CancellationToken cancellationToken)
    {
        var entidadesVisibles = await ambito.EntidadesVisiblesAsync(cancellationToken);
        if (entidadAcademicaId is { } filtro && entidadesVisibles is not null && !entidadesVisibles.Contains(filtro))
        {
            return contexto.Set<ExperienciaEducativa>().Where(_ => false).Select(e => e.Id);
        }

        IReadOnlyCollection<int>? entidades = entidadAcademicaId is { } entidad ? [entidad] : entidadesVisibles;
        var experiencias = contexto.Set<ExperienciaEducativa>()
            .IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
            .AsNoTracking();
        var planes = contexto.Set<PlanEstudios>()
            .IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
            .AsNoTracking();
        var programas = contexto.Set<ProgramaEducativo>()
            .IgnoreQueryFilters([FiltrosConsulta.BajaLogica])
            .AsNoTracking();

        var consulta = from experiencia in experiencias
                       join plan in planes on experiencia.PlanEstudiosId equals plan.Id
                       join programa in programas on plan.ProgramaEducativoId equals programa.Id
                       select new { experiencia.Id, programa.EntidadAcademicaId };

        if (entidades is not null)
        {
            consulta = consulta.Where(e => entidades.Contains(e.EntidadAcademicaId));
        }

        return consulta.Select(e => e.Id);
    }

    private static async Task<IReadOnlyDictionary<int, ExperienciaEducativaResumen>> ArmarRespuestasAsync(
        IReadOnlyCollection<ExperienciaEducativaIntermedia> datos,
        IAmbitosInstitucionales ambitosInstitucionales,
        IClasificacionesAcademicas clasificaciones,
        CancellationToken cancellationToken)
    {
        if (datos.Count == 0)
        {
            return new Dictionary<int, ExperienciaEducativaResumen>();
        }

        var entidades = await ambitosInstitucionales.ObtenerEntidadesAsync(
            datos.Select(e => e.EntidadAcademicaId).Distinct().ToArray(), cancellationToken);
        var sistemas = await clasificaciones.ObtenerSistemasEducativosAsync(
            datos.Select(e => e.SistemaEducativoId).Distinct().ToArray(), cancellationToken);

        return datos.ToDictionary(e => e.Id, e =>
        {
            var entidad = entidades[e.EntidadAcademicaId];
            var sistema = sistemas[e.SistemaEducativoId];
            return new ExperienciaEducativaResumen(
                e.Id,
                e.Materia,
                e.Curso,
                e.Nombre,
                e.CupoMinimo,
                e.CupoMaximo,
                e.Vigente,
                e.PlanEstudiosId,
                e.PlanEstudiosCodigo,
                e.ProgramaEducativoId,
                e.ProgramaEducativoNombre,
                e.SistemaEducativoId,
                sistema.Nombre,
                e.EntidadAcademicaId,
                entidad.Clave,
                entidad.Nombre,
                entidad.AreaAcademicaId);
        });
    }

    private sealed record ExperienciaEducativaIntermedia(
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
        int EntidadAcademicaId);
}
