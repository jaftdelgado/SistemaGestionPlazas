using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Archivos;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.SolicitudesApertura.Infrastructure.SolicitudesApertura;

internal sealed class ListarSolicitudesAperturaHandler(
    SgplaDbContext contexto,
    IExperienciasEducativas experienciasEducativas,
    IPeriodosEscolares periodosEscolares)
    : IQueryHandler<ListarSolicitudesAperturaQuery, Pagina<SolicitudAperturaResponse>>
{
    public async Task<Result<Pagina<SolicitudAperturaResponse>>> HandleAsync(
        ListarSolicitudesAperturaQuery query,
        CancellationToken cancellationToken)
    {
        var visibles = await experienciasEducativas.ConsultarIdsVisiblesAsync(
            query.Filtros.EntidadAcademicaId, cancellationToken);
        var solicitudes = contexto.Set<SolicitudApertura>()
            .AsNoTracking()
            .Where(s => visibles.Contains(s.ExperienciaEducativaId));

        if (query.Filtros.Estado is { } estado)
        {
            var estadoNormalizado = Enum.Parse<EstadoSolicitudApertura>(estado, ignoreCase: true);
            solicitudes = solicitudes.Where(s => s.Estado == estadoNormalizado);
        }

        if (query.Filtros.PeriodoEscolarId is { } periodoId)
        {
            solicitudes = solicitudes.Where(s => s.PeriodoEscolarId == periodoId);
        }

        if (query.Filtros.ExperienciaEducativaId is { } experienciaId)
        {
            solicitudes = solicitudes.Where(s => s.ExperienciaEducativaId == experienciaId);
        }

        var pagina = await solicitudes
            .OrderByDescending(s => s.CreadaEn)
            .ThenByDescending(s => s.Id)
            .Select(SolicitudAperturaIntermedia.Proyeccion)
            .PaginarAsync(query.Paginacion, cancellationToken);

        var elementos = await SolicitudAperturaIntermedia.ArmarRespuestasAsync(
            pagina.Elementos, experienciasEducativas, periodosEscolares, cancellationToken);
        return new Pagina<SolicitudAperturaResponse>(elementos, pagina.NumeroPagina, pagina.TamanoPagina, pagina.Total);
    }
}

internal sealed class ObtenerSolicitudAperturaHandler(
    SgplaDbContext contexto,
    IExperienciasEducativas experienciasEducativas,
    IPeriodosEscolares periodosEscolares)
    : IQueryHandler<ObtenerSolicitudAperturaQuery, SolicitudAperturaResponse>
{
    public async Task<Result<SolicitudAperturaResponse>> HandleAsync(
        ObtenerSolicitudAperturaQuery query,
        CancellationToken cancellationToken)
    {
        var visibles = await experienciasEducativas.ConsultarIdsVisiblesAsync(null, cancellationToken);
        var solicitud = await contexto.Set<SolicitudApertura>()
            .AsNoTracking()
            .Where(s => s.Id == query.Id && visibles.Contains(s.ExperienciaEducativaId))
            .Select(SolicitudAperturaIntermedia.Proyeccion)
            .FirstOrDefaultAsync(cancellationToken);

        if (solicitud is null)
        {
            return SolicitudAperturaErrors.NoEncontrada(query.Id);
        }

        var respuestas = await SolicitudAperturaIntermedia.ArmarRespuestasAsync(
            [solicitud], experienciasEducativas, periodosEscolares, cancellationToken);
        return respuestas[0];
    }
}

internal sealed class DescargarOficioSolicitudAperturaHandler(
    SgplaDbContext contexto,
    IExperienciasEducativas experienciasEducativas,
    IAlmacenamientoArchivos almacenamiento)
    : IQueryHandler<DescargarOficioSolicitudAperturaQuery, OficioDescarga>
{
    public async Task<Result<OficioDescarga>> HandleAsync(
        DescargarOficioSolicitudAperturaQuery query,
        CancellationToken cancellationToken)
    {
        var visibles = await experienciasEducativas.ConsultarIdsVisiblesAsync(null, cancellationToken);
        var archivo = await contexto.Set<SolicitudApertura>()
            .AsNoTracking()
            .Where(s => s.Id == query.Id && visibles.Contains(s.ExperienciaEducativaId))
            .Select(s => new { s.Oficio.Nombre, s.Oficio.ClaveAlmacenamiento })
            .FirstOrDefaultAsync(cancellationToken);

        if (archivo is null)
        {
            return SolicitudAperturaErrors.NoEncontrada(query.Id);
        }

        var contenido = await almacenamiento.AbrirAsync(archivo.ClaveAlmacenamiento, cancellationToken);
        return contenido is null
            ? throw new InvalidOperationException($"No se encontró el binario del oficio de la solicitud de apertura {query.Id}.")
            : new OficioDescarga(contenido, archivo.Nombre);
    }
}

internal sealed record SolicitudAperturaIntermedia(
    int Id,
    EstadoSolicitudApertura Estado,
    string Seccion,
    int CantidadEstudiantes,
    string Justificacion,
    int ExperienciaEducativaId,
    int PeriodoEscolarId,
    string NombreOficio,
    long TamanoOficio,
    DateTime CargadoEn,
    DateTime CreadaEn,
    int CreadaPorUsuarioId,
    DateTime? ActualizadaEn,
    int? ActualizadaPorUsuarioId,
    DateTime? ResueltaEn,
    int? ResueltaPorUsuarioId,
    string? ComentariosResolucion,
    DateTime? CanceladaEn,
    int? CanceladaPorUsuarioId,
    string? MotivoCancelacion)
{
    public static Expression<Func<SolicitudApertura, SolicitudAperturaIntermedia>> Proyeccion => s => new(
        s.Id,
        s.Estado,
        s.Seccion,
        s.CantidadEstudiantes,
        s.Justificacion,
        s.ExperienciaEducativaId,
        s.PeriodoEscolarId,
        s.Oficio.Nombre,
        s.Oficio.Tamano,
        s.Oficio.CargadoEn,
        s.CreadaEn,
        s.CreadaPorUsuarioId,
        s.ActualizadaEn,
        s.ActualizadaPorUsuarioId,
        s.ResueltaEn,
        s.ResueltaPorUsuarioId,
        s.ComentariosResolucion,
        s.CanceladaEn,
        s.CanceladaPorUsuarioId,
        s.MotivoCancelacion);

    public static async Task<List<SolicitudAperturaResponse>> ArmarRespuestasAsync(
        IReadOnlyCollection<SolicitudAperturaIntermedia> solicitudes,
        IExperienciasEducativas experienciasEducativas,
        IPeriodosEscolares periodosEscolares,
        CancellationToken cancellationToken)
    {
        if (solicitudes.Count == 0)
        {
            return [];
        }

        var experiencias = await experienciasEducativas.ObtenerAsync(
            solicitudes.Select(s => s.ExperienciaEducativaId).Distinct().ToArray(), cancellationToken);
        var periodos = await periodosEscolares.ObtenerAsync(
            solicitudes.Select(s => s.PeriodoEscolarId).Distinct().ToArray(), cancellationToken);

        return solicitudes.Select(s =>
        {
            var experiencia = experiencias[s.ExperienciaEducativaId];
            var periodo = periodos[s.PeriodoEscolarId];

            return new SolicitudAperturaResponse(
                s.Id,
                s.Estado.ToString().ToUpperInvariant(),
                s.Seccion,
                s.CantidadEstudiantes,
                s.Justificacion,
                new ExperienciaSolicitadaResponse(
                    experiencia.Id,
                    experiencia.Materia,
                    experiencia.Curso,
                    experiencia.Nombre,
                    experiencia.CupoMinimo,
                    experiencia.CupoMaximo,
                    experiencia.PlanEstudiosId,
                    experiencia.PlanEstudiosCodigo,
                    experiencia.ProgramaEducativoId,
                    experiencia.ProgramaEducativoNombre,
                    experiencia.EntidadAcademicaId,
                    experiencia.EntidadAcademicaClave,
                    experiencia.EntidadAcademicaNombre,
                    experiencia.SistemaEducativoId,
                    experiencia.SistemaEducativoNombre),
                new PeriodoSolicitadoResponse(periodo.Id, periodo.Clave),
                new OficioResponse(s.NombreOficio, s.TamanoOficio, s.CargadoEn),
                s.CreadaEn,
                s.CreadaPorUsuarioId,
                s.ActualizadaEn,
                s.ActualizadaPorUsuarioId,
                s.ResueltaEn,
                s.ResueltaPorUsuarioId,
                s.ComentariosResolucion,
                s.CanceladaEn,
                s.CanceladaPorUsuarioId,
                s.MotivoCancelacion);
        }).ToList();
    }
}
