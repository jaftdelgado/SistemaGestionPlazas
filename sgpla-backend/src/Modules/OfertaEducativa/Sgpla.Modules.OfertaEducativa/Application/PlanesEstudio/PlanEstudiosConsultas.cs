using FluentValidation;
using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.OfertaEducativa.Application.ProgramasEducativos;

namespace Sgpla.Modules.OfertaEducativa.Application.PlanesEstudio;

internal sealed record ProgramaEducativoResumenResponse(int Id, string Nombre);

internal sealed record PlanEstudiosResponse(
    int Id,
    string Codigo,
    ProgramaEducativoResumenResponse ProgramaEducativo,
    EntidadAcademicaResumenResponse EntidadAcademica,
    int ExperienciasEducativas);

/// <summary>Filtros opcionales, combinados con AND.</summary>
internal sealed record FiltrosPlanesEstudio(int? ProgramaEducativoId, int? EntidadAcademicaId, string? Busqueda);

/// <summary>Planes activos del ámbito, en orden de código (con el id como desempate), paginados.</summary>
internal sealed record ListarPlanesEstudioQuery(Paginacion Paginacion, FiltrosPlanesEstudio Filtros);

internal sealed class ListarPlanesEstudioValidator : AbstractValidator<ListarPlanesEstudioQuery>
{
    public ListarPlanesEstudioValidator()
    {
        Include(new PaginacionValidator<ListarPlanesEstudioQuery>(q => q.Paginacion));

        RuleFor(q => q.Filtros.ProgramaEducativoId).GreaterThan(0)
            .OverridePropertyName("programaEducativoId").WithName("programa educativo")
            .When(q => q.Filtros.ProgramaEducativoId is not null);
        RuleFor(q => q.Filtros.EntidadAcademicaId).GreaterThan(0)
            .OverridePropertyName("entidadAcademicaId").WithName("entidad académica")
            .When(q => q.Filtros.EntidadAcademicaId is not null);

        RuleFor(q => q.Filtros.Busqueda).MaximumLength(200)
            .OverridePropertyName("busqueda").WithName("búsqueda").When(q => q.Filtros.Busqueda is not null);
    }
}

internal sealed record ObtenerPlanEstudiosQuery(int Id);
