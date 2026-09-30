using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.OfertaEducativa.Application.Ambito;
using Sgpla.Modules.OfertaEducativa.Application.PlanesEstudio;
using Sgpla.Modules.OfertaEducativa.Domain.PlanesEstudio;
using Sgpla.Modules.OfertaEducativa.Domain.ProgramasEducativos;
using Sgpla.Modules.OfertaEducativa.Infrastructure.ProgramasEducativos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.OfertaEducativa.Infrastructure.PlanesEstudio;

/// <summary>
/// Genera el .xlsx del plan con el formato de la UV: la hoja <c>Hoja1</c>, los 14
/// encabezados en la fila 1 y una fila por EE activa. Es el inverso de las reglas de mapeo de la importación. No ajusta el
/// ancho de las columnas, porque eso depende de las fuentes instaladas en el sistema.
/// </summary>
internal sealed class ExportarPlanEstudiosHandler(
    SgplaDbContext contexto,
    IAmbitoOfertaEducativa ambito,
    IAmbitosInstitucionales ambitosInstitucionales,
    IClasificacionesAcademicas clasificaciones)
    : IQueryHandler<ExportarPlanEstudiosQuery, ArchivoGenerado>
{
    private const string NombreHoja = "Hoja1";

    private static readonly string[] Encabezados =
    [
        "DESC_AREA_ACAD",
        "CODIGO_PLAN",
        "DESCRIPCION",
        "CODIGO_PER_CAT",
        "DESC_PER_CAT",
        "MATERIA_EE",
        "CURSO_EE",
        "DESC_EE",
        "HT_EE",
        "HP_EE",
        "CREDITOS_EE",
        "CODE_AREA_F",
        "DESC_AREA_F",
        "PERFIL_DOC",
    ];

    public async Task<Result<ArchivoGenerado>> HandleAsync(ExportarPlanEstudiosQuery query, CancellationToken cancellationToken)
    {
        var programas = await ProgramaEducativoAmbito.AplicarAsync(
            contexto.Set<ProgramaEducativo>().AsNoTracking(), ambito, cancellationToken);

        var plan = await (
            from p in contexto.Set<PlanEstudios>().AsNoTracking()
            join programa in programas on p.ProgramaEducativoId equals programa.Id
            where p.Id == query.Id
            select new { p.Id, p.Codigo, ProgramaNombre = programa.Nombre, programa.EntidadAcademicaId })
            .FirstOrDefaultAsync(cancellationToken);

        if (plan is null)
        {
            return PlanEstudiosErrors.NoEncontrado(query.Id);
        }

        var experiencias = await contexto.Set<ExperienciaEducativa>()
            .AsNoTracking()
            .Where(e => e.PlanEstudiosId == plan.Id)
            .OrderBy(e => e.Materia)
            .ThenBy(e => e.Curso)
            .ThenBy(e => e.Id)
            .Select(e => new FilaExperiencia(
                e.Materia, e.Curso, e.Nombre, e.HorasTeoricas, e.HorasPracticas, e.Creditos, e.AreaFormacionId, e.PerfilDocente))
            .ToListAsync(cancellationToken);

        var entidades = await ambitosInstitucionales.ObtenerEntidadesAsync([plan.EntidadAcademicaId], cancellationToken);
        var entidad = entidades[plan.EntidadAcademicaId];
        var areas = await ambitosInstitucionales.ObtenerAreasAsync([entidad.AreaAcademicaId], cancellationToken);
        var areaAcademica = areas[entidad.AreaAcademicaId];
        var areasFormacion = await clasificaciones.ObtenerAreasFormacionAsync(
            experiencias.Select(e => e.AreaFormacionId).Distinct().ToList(), cancellationToken);

        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add(NombreHoja);

        for (var columna = 0; columna < Encabezados.Length; columna++)
        {
            hoja.Cell(1, columna + 1).Value = Encabezados[columna];
        }

        for (var i = 0; i < experiencias.Count; i++)
        {
            var experiencia = experiencias[i];
            var areaFormacion = areasFormacion[experiencia.AreaFormacionId];
            var fila = i + 2;

            // Texto explícito en las claves, para conservar los ceros iniciales; las columnas 4 y 5 quedan vacías.
            hoja.Cell(fila, 1).Value = areaAcademica.Nombre;
            hoja.Cell(fila, 2).Value = plan.Codigo;
            hoja.Cell(fila, 3).Value = plan.ProgramaNombre;
            hoja.Cell(fila, 6).Value = experiencia.Materia;
            hoja.Cell(fila, 7).Value = experiencia.Curso;
            hoja.Cell(fila, 8).Value = experiencia.Nombre;
            hoja.Cell(fila, 9).Value = experiencia.HorasTeoricas;
            hoja.Cell(fila, 10).Value = experiencia.HorasPracticas;
            hoja.Cell(fila, 11).Value = experiencia.Creditos;
            hoja.Cell(fila, 12).Value = areaFormacion.Clave;
            hoja.Cell(fila, 13).Value = areaFormacion.Nombre;

            if (experiencia.PerfilDocente is not null)
            {
                hoja.Cell(fila, 14).Value = experiencia.PerfilDocente;
            }
        }

        using var flujo = new MemoryStream();
        libro.SaveAs(flujo);

        return new ArchivoGenerado($"{plan.Codigo}.xlsx", ArchivoGenerado.TipoContenidoExcel, flujo.ToArray());
    }

    private sealed record FilaExperiencia(
        string Materia,
        string Curso,
        string Nombre,
        int HorasTeoricas,
        int HorasPracticas,
        int Creditos,
        int AreaFormacionId,
        string? PerfilDocente);
}
