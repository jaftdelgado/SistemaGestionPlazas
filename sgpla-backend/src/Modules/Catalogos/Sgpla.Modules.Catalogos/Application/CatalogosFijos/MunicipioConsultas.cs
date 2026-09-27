using FluentValidation;
using Sgpla.BuildingBlocks.Application;

namespace Sgpla.Modules.Catalogos.Application.CatalogosFijos;

/// <summary>
/// Municipios en orden alfabético, paginados; con <paramref name="Busqueda"/>, solo los que la contienen en su nombre.
/// </summary>
/// <param name="Busqueda">
/// Filtro opcional por nombre, ya normalizado por el endpoint (recortado y sin espacios repetidos): nunca llega
/// vacío.
/// </param>
internal sealed record ListarMunicipiosQuery(Paginacion Paginacion, string? Busqueda);

internal sealed class ListarMunicipiosValidator : AbstractValidator<ListarMunicipiosQuery>
{
    public ListarMunicipiosValidator()
    {
        Include(new PaginacionValidator<ListarMunicipiosQuery>(q => q.Paginacion));

        RuleFor(q => q.Busqueda)
            .MaximumLength(150)
            .OverridePropertyName("busqueda")
            .WithName("búsqueda")
            .When(q => q.Busqueda is not null);
    }
}

internal sealed record ObtenerMunicipioQuery(int Id);
