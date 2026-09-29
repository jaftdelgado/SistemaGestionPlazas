using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Application.Ambito;
using Sgpla.Modules.OfertaEducativa.Application.ProgramasEducativos;
using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.ProgramasEducativos;

internal sealed class ListarProgramasEducativosHandler(
    SgplaDbContext contexto,
    IAmbitoOfertaEducativa ambito,
    IAmbitosInstitucionales ambitosInstitucionales,
    IClasificacionesAcademicas clasificaciones)
    : IQueryHandler<ListarProgramasEducativosQuery, Pagina<ProgramaEducativoResponse>>
{
    public async Task<Result<Pagina<ProgramaEducativoResponse>>> HandleAsync(
        ListarProgramasEducativosQuery query,
        CancellationToken cancellationToken)
    {
        var filtros = query.Filtros;

        // El ámbito va antes que los demás filtros.
        var programas = await ProgramaEducativoAmbito.AplicarAsync(
            contexto.Set<ProgramaEducativo>().AsNoTracking(), ambito, cancellationToken);

        if (filtros.EntidadAcademicaId is not null)
        {
            programas = programas.Where(p => p.EntidadAcademicaId == filtros.EntidadAcademicaId);
        }

        if (filtros.SistemaEducativoId is not null)
        {
            programas = programas.Where(p => p.SistemaEducativoId == filtros.SistemaEducativoId);
        }

        if (filtros.NivelFormacionId is not null)
        {
            programas = programas.Where(p => p.NivelFormacionId == filtros.NivelFormacionId);
        }

        var busqueda = Normalizacion.Texto(filtros.Busqueda);
        if (busqueda.Length > 0)
        {
            // La columna ya ignora mayúsculas y acentos.
            programas = programas.Where(p => p.Nombre.Contains(busqueda));
        }

        var pagina = await programas
            .OrderBy(p => p.Nombre)
            .ThenBy(p => p.Id)
            .Select(p => new ProgramaEducativoIntermedio(
                p.Id, p.Nombre, p.EntidadAcademicaId, p.SistemaEducativoId, p.NivelFormacionId))
            .PaginarAsync(query.Paginacion, cancellationToken);

        var elementos = await ProgramaEducativoIntermedio.ArmarRespuestasAsync(
            pagina.Elementos, ambitosInstitucionales, clasificaciones, cancellationToken);

        return new Pagina<ProgramaEducativoResponse>(elementos, pagina.NumeroPagina, pagina.TamanoPagina, pagina.Total);
    }
}

internal sealed class ObtenerProgramaEducativoHandler(
    SgplaDbContext contexto,
    IAmbitoOfertaEducativa ambito,
    IAmbitosInstitucionales ambitosInstitucionales,
    IClasificacionesAcademicas clasificaciones)
    : IQueryHandler<ObtenerProgramaEducativoQuery, ProgramaEducativoResponse>
{
    public async Task<Result<ProgramaEducativoResponse>> HandleAsync(
        ObtenerProgramaEducativoQuery query,
        CancellationToken cancellationToken)
    {
        var programas = await ProgramaEducativoAmbito.AplicarAsync(
            contexto.Set<ProgramaEducativo>().AsNoTracking(), ambito, cancellationToken);

        var encontrado = await programas
            .Where(p => p.Id == query.Id)
            .Select(p => new ProgramaEducativoIntermedio(
                p.Id, p.Nombre, p.EntidadAcademicaId, p.SistemaEducativoId, p.NivelFormacionId))
            .FirstOrDefaultAsync(cancellationToken);

        if (encontrado is null)
        {
            return ProgramaEducativoErrors.NoEncontrado(query.Id);
        }

        var respuestas = await ProgramaEducativoIntermedio.ArmarRespuestasAsync(
            [encontrado], ambitosInstitucionales, clasificaciones, cancellationToken);
        return respuestas[0];
    }
}

/// <summary>Restringe los programas a las entidades que el usuario en curso puede consultar (sección 6).</summary>
internal static class ProgramaEducativoAmbito
{
    public static async Task<IQueryable<ProgramaEducativo>> AplicarAsync(
        IQueryable<ProgramaEducativo> programas,
        IAmbitoOfertaEducativa ambito,
        CancellationToken cancellationToken)
    {
        var entidades = await ambito.EntidadesVisiblesAsync(cancellationToken);
        return entidades is null ? programas : programas.Where(p => entidades.Contains(p.EntidadAcademicaId));
    }
}

/// <summary>Programa con solo los ids; los nombres se resuelven con una llamada por contrato, sobre toda la página.</summary>
internal sealed record ProgramaEducativoIntermedio(
    int Id,
    string Nombre,
    int EntidadAcademicaId,
    int SistemaEducativoId,
    int NivelFormacionId)
{
    public static async Task<List<ProgramaEducativoResponse>> ArmarRespuestasAsync(
        IReadOnlyList<ProgramaEducativoIntermedio> programas,
        IAmbitosInstitucionales ambitosInstitucionales,
        IClasificacionesAcademicas clasificaciones,
        CancellationToken cancellationToken)
    {
        var entidades = await ambitosInstitucionales.ObtenerEntidadesAsync(
            programas.Select(p => p.EntidadAcademicaId).Distinct().ToList(), cancellationToken);
        var sistemas = await clasificaciones.ObtenerSistemasEducativosAsync(
            programas.Select(p => p.SistemaEducativoId).Distinct().ToList(), cancellationToken);
        var niveles = await clasificaciones.ObtenerNivelesFormacionAsync(
            programas.Select(p => p.NivelFormacionId).Distinct().ToList(), cancellationToken);

        return programas.Select(p =>
        {
            var entidad = entidades[p.EntidadAcademicaId];
            var sistema = sistemas[p.SistemaEducativoId];
            var nivel = niveles[p.NivelFormacionId];

            return new ProgramaEducativoResponse(
                p.Id,
                p.Nombre,
                new EntidadAcademicaResumenResponse(entidad.Id, entidad.Clave, entidad.Nombre),
                new SistemaEducativoResponse(sistema.Id, sistema.Nombre),
                new NivelFormacionResponse(nivel.Id, nivel.Clave, nivel.Nombre));
        }).ToList();
    }
}
