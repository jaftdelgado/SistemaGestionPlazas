using FluentValidation;
using Sgpla.BuildingBlocks.Application;

namespace Sgpla.Modules.OfertaEducativa.Application.ProgramasEducativos;

internal sealed record EntidadAcademicaResumenResponse(int Id, string Clave, string Nombre);

internal sealed record SistemaEducativoResponse(int Id, string Nombre);

internal sealed record NivelFormacionResponse(int Id, string Clave, string Nombre);

internal sealed record ProgramaEducativoResponse(
    int Id,
    string Nombre,
    EntidadAcademicaResumenResponse EntidadAcademica,
    SistemaEducativoResponse SistemaEducativo,
    NivelFormacionResponse NivelFormacion);

/// <summary>Filtros opcionales, combinados con AND. Un texto vacío o solo con espacios se ignora.</summary>
internal sealed record FiltrosProgramasEducativos(
    int? EntidadAcademicaId,
    int? SistemaEducativoId,
    int? NivelFormacionId,
    string? Busqueda);

/// <summary>Programas activos del ámbito, en orden de nombre (con el id como desempate), paginados.</summary>
internal sealed record ListarProgramasEducativosQuery(Paginacion Paginacion, FiltrosProgramasEducativos Filtros);

internal sealed class ListarProgramasEducativosValidator : AbstractValidator<ListarProgramasEducativosQuery>
{
    public ListarProgramasEducativosValidator()
    {
        Include(new PaginacionValidator<ListarProgramasEducativosQuery>(q => q.Paginacion));

        RuleFor(q => q.Filtros.EntidadAcademicaId).GreaterThan(0)
            .OverridePropertyName("entidadAcademicaId").WithName("entidad académica")
            .When(q => q.Filtros.EntidadAcademicaId is not null);
        RuleFor(q => q.Filtros.SistemaEducativoId).GreaterThan(0)
            .OverridePropertyName("sistemaEducativoId").WithName("sistema educativo")
            .When(q => q.Filtros.SistemaEducativoId is not null);
        RuleFor(q => q.Filtros.NivelFormacionId).GreaterThan(0)
            .OverridePropertyName("nivelFormacionId").WithName("nivel de formación")
            .When(q => q.Filtros.NivelFormacionId is not null);

        RuleFor(q => q.Filtros.Busqueda).MaximumLength(200)
            .OverridePropertyName("busqueda").WithName("búsqueda").When(q => q.Filtros.Busqueda is not null);
    }
}

internal sealed record ObtenerProgramaEducativoQuery(int Id);
