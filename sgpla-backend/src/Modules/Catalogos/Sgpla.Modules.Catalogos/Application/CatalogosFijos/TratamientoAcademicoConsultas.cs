using FluentValidation;

namespace Sgpla.Modules.Catalogos.Application.CatalogosFijos;

internal sealed record TratamientoAcademicoResponse(int Id, string Nombre, CatalogoFijoResponse GradoAcademico);

/// <summary>Los tratamientos en orden de id; con <paramref name="GradoAcademicoId"/>, solo los de ese grado.</summary>
/// <param name="GradoAcademicoId">Filtro opcional. Un grado inexistente no es error: el listado queda vacío.</param>
internal sealed record ListarTratamientosAcademicosQuery(int? GradoAcademicoId);

internal sealed class ListarTratamientosAcademicosValidator : AbstractValidator<ListarTratamientosAcademicosQuery>
{
    public ListarTratamientosAcademicosValidator()
    {
        RuleFor(q => q.GradoAcademicoId).GreaterThan(0).WithName("grado académico").When(q => q.GradoAcademicoId is not null);
    }
}

internal sealed record ObtenerTratamientoAcademicoQuery(int Id);
