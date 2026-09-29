namespace Sgpla.Modules.OfertaEducativa.Application.ExperienciasEducativas;

internal sealed record AreaFormacionResponse(int Id, string Clave, string Nombre);

internal sealed record PlanEstudiosResumenResponse(int Id, string Codigo);

internal sealed record ExperienciaEducativaResponse(
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
    AreaFormacionResponse AreaFormacion,
    PlanEstudiosResumenResponse PlanEstudios,
    bool TuvoProgramaciones);

/// <summary>EE activas de un plan activo del ámbito, en orden de materia, curso e id. 404 si el plan no está a la vista.</summary>
internal sealed record ListarExperienciasEducativasDePlanQuery(int PlanEstudiosId);
