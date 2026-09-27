using FluentValidation;

namespace Sgpla.Modules.Institucional.Application.Ubicaciones;

internal sealed record RegionResponse(int Id, int Clave, string Nombre);

internal sealed record CampusResponse(int Id, string Clave, string Nombre, RegionResponse Region);

/// <summary>Todas las regiones, en orden de clave.</summary>
internal sealed record ListarRegionesQuery;

internal sealed record ObtenerRegionQuery(int Id);

/// <summary>Los campus en orden de clave; con <paramref name="RegionId"/>, solo los de esa región.</summary>
/// <param name="RegionId">Filtro opcional. Una región inexistente no es error: el listado queda vacío.</param>
internal sealed record ListarCampusQuery(int? RegionId);

internal sealed class ListarCampusValidator : AbstractValidator<ListarCampusQuery>
{
    public ListarCampusValidator()
    {
        RuleFor(q => q.RegionId).GreaterThan(0).WithName("región").When(q => q.RegionId is not null);
    }
}

internal sealed record ObtenerCampusQuery(int Id);
