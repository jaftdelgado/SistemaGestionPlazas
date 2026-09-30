using FluentValidation;
using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.OfertaEducativa.Application.ExperienciasEducativas;
using Sgpla.Modules.OfertaEducativa.Application.PlanesEstudio;
using Sgpla.Modules.OfertaEducativa.Application.ProgramasEducativos;
using Sgpla.Modules.OfertaEducativa.Domain.Programaciones;

namespace Sgpla.Modules.OfertaEducativa.Application.Programaciones;

internal sealed record PeriodoEscolarResumenResponse(int Id, string Clave);

internal sealed record ExperienciaEducativaResumenResponse(int Id, string Materia, string Curso, string Nombre);

internal sealed record ProgramacionAcademicaResponse(
    int Id,
    string Nrc,
    PeriodoEscolarResumenResponse PeriodoEscolar,
    ExperienciaEducativaResumenResponse ExperienciaEducativa,
    PlanEstudiosResumenResponse PlanEstudios,
    ProgramaEducativoResumenResponse ProgramaEducativo,
    EntidadAcademicaResumenResponse EntidadAcademica);

internal sealed record HorarioResponse(
    int Id,
    byte DiaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    string? Edificio,
    string? Aula);

/// <summary>Filtros opcionales, combinados con AND. <paramref name="Nrc"/> se recorta, se pasa a mayúsculas y se compara exacto. Un NRC vacío o solo con espacios se ignora.</summary>
internal sealed record FiltrosProgramacionesAcademicas(
    int? PeriodoEscolarId,
    int? EntidadAcademicaId,
    int? ProgramaEducativoId,
    int? PlanEstudiosId,
    int? ExperienciaEducativaId,
    string? Nrc);

/// <summary>Programaciones activas del ámbito, por clave de periodo descendente, NRC e id, paginadas.</summary>
internal sealed record ListarProgramacionesAcademicasQuery(Paginacion Paginacion, FiltrosProgramacionesAcademicas Filtros);

internal sealed class ListarProgramacionesAcademicasValidator : AbstractValidator<ListarProgramacionesAcademicasQuery>
{
    public ListarProgramacionesAcademicasValidator()
    {
        Include(new PaginacionValidator<ListarProgramacionesAcademicasQuery>(q => q.Paginacion));

        RuleFor(q => q.Filtros.PeriodoEscolarId).GreaterThan(0)
            .OverridePropertyName("periodoEscolarId").WithName("periodo escolar")
            .When(q => q.Filtros.PeriodoEscolarId is not null);
        RuleFor(q => q.Filtros.EntidadAcademicaId).GreaterThan(0)
            .OverridePropertyName("entidadAcademicaId").WithName("entidad académica")
            .When(q => q.Filtros.EntidadAcademicaId is not null);
        RuleFor(q => q.Filtros.ProgramaEducativoId).GreaterThan(0)
            .OverridePropertyName("programaEducativoId").WithName("programa educativo")
            .When(q => q.Filtros.ProgramaEducativoId is not null);
        RuleFor(q => q.Filtros.PlanEstudiosId).GreaterThan(0)
            .OverridePropertyName("planEstudiosId").WithName("plan de estudios")
            .When(q => q.Filtros.PlanEstudiosId is not null);
        RuleFor(q => q.Filtros.ExperienciaEducativaId).GreaterThan(0)
            .OverridePropertyName("experienciaEducativaId").WithName("experiencia educativa")
            .When(q => q.Filtros.ExperienciaEducativaId is not null);

        RuleFor(q => q.Filtros.Nrc).MaximumLength(ProgramacionAcademica.LongitudMaximaNrc)
            .OverridePropertyName("nrc").WithName("NRC").When(q => q.Filtros.Nrc is not null);
    }
}

internal sealed record ObtenerProgramacionAcademicaQuery(int Id);

/// <summary>Sesiones vigentes de una programación del ámbito, por día, hora de inicio e id. 404 si no está a la vista.</summary>
internal sealed record ListarHorariosQuery(int ProgramacionAcademicaId);
