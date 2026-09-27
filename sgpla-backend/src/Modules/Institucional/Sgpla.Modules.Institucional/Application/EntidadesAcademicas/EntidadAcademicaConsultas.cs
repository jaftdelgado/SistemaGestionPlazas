using FluentValidation;
using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Institucional.Application.Ubicaciones;

namespace Sgpla.Modules.Institucional.Application.EntidadesAcademicas;

internal sealed record ReferenciaResponse(int Id, string Nombre);

internal sealed record AreaAcademicaResumenResponse(int Id, int Clave, string Nombre);

internal sealed record EntidadAcademicaResponse(
    int Id,
    string Clave,
    string Nombre,
    string Calle,
    string? NumeroExterior,
    string Colonia,
    string CodigoPostal,
    string Telefono,
    string? Extension,
    CampusResponse Campus,
    AreaAcademicaResumenResponse AreaAcademica,
    ReferenciaResponse Municipio);

/// <summary>Filtros opcionales del listado; todos se combinan con AND. Un texto vacío o solo con espacios se ignora.</summary>
internal sealed record FiltrosEntidadesAcademicas(
    int? RegionId,
    int? CampusId,
    int? AreaAcademicaId,
    int? MunicipioId,
    string? Busqueda,
    string? Calle,
    string? Colonia,
    string? NumeroExterior,
    string? CodigoPostal,
    string? Telefono);

/// <summary>Entidades activas en orden de clave (con el id como desempate), paginadas.</summary>
internal sealed record ListarEntidadesAcademicasQuery(Paginacion Paginacion, FiltrosEntidadesAcademicas Filtros);

internal sealed class ListarEntidadesAcademicasValidator : AbstractValidator<ListarEntidadesAcademicasQuery>
{
    public ListarEntidadesAcademicasValidator()
    {
        Include(new PaginacionValidator<ListarEntidadesAcademicasQuery>(q => q.Paginacion));

        RuleFor(q => q.Filtros.RegionId).GreaterThan(0)
            .OverridePropertyName("regionId").WithName("región").When(q => q.Filtros.RegionId is not null);
        RuleFor(q => q.Filtros.CampusId).GreaterThan(0)
            .OverridePropertyName("campusId").WithName("campus").When(q => q.Filtros.CampusId is not null);
        RuleFor(q => q.Filtros.AreaAcademicaId).GreaterThan(0)
            .OverridePropertyName("areaAcademicaId").WithName("área académica").When(q => q.Filtros.AreaAcademicaId is not null);
        RuleFor(q => q.Filtros.MunicipioId).GreaterThan(0)
            .OverridePropertyName("municipioId").WithName("municipio").When(q => q.Filtros.MunicipioId is not null);

        RuleFor(q => q.Filtros.Busqueda).MaximumLength(200)
            .OverridePropertyName("busqueda").WithName("búsqueda").When(q => q.Filtros.Busqueda is not null);
        RuleFor(q => q.Filtros.Calle).MaximumLength(200)
            .OverridePropertyName("calle").WithName("calle").When(q => q.Filtros.Calle is not null);
        RuleFor(q => q.Filtros.Colonia).MaximumLength(200)
            .OverridePropertyName("colonia").WithName("colonia").When(q => q.Filtros.Colonia is not null);
        RuleFor(q => q.Filtros.NumeroExterior).MaximumLength(200)
            .OverridePropertyName("numeroExterior").WithName("número exterior").When(q => q.Filtros.NumeroExterior is not null);

        RuleFor(q => q.Filtros.CodigoPostal)
            .Must(codigoPostal => codigoPostal!.Trim().Length == 5 && codigoPostal.Trim().All(char.IsAsciiDigit))
            .OverridePropertyName("codigoPostal").WithName("código postal")
            .When(q => !string.IsNullOrWhiteSpace(q.Filtros.CodigoPostal));

        RuleFor(q => q.Filtros.Telefono)
            .Must(telefono => telefono!.Trim().Length == 10 && telefono.Trim().All(char.IsAsciiDigit))
            .OverridePropertyName("telefono").WithName("teléfono")
            .When(q => !string.IsNullOrWhiteSpace(q.Filtros.Telefono));
    }
}

internal sealed record ObtenerEntidadAcademicaQuery(int Id);
