using FluentValidation;
using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Usuarios.Application.Sesion;

namespace Sgpla.Modules.Usuarios.Application.Cuentas;

internal sealed record CuentaResponse(
    int Id,
    string Correo,
    string Nombre,
    RolResponse Rol,
    AreaAcademicaResumenResponse? AreaAcademica,
    EntidadAcademicaResumenResponse? EntidadAcademica);

/// <summary>Filtros opcionales, combinados con AND. Un texto vacío o solo con espacios se ignora.</summary>
internal sealed record FiltrosCuentas(byte? RolId, int? AreaAcademicaId, int? EntidadAcademicaId, string? Busqueda);

/// <summary>Cuentas activas en orden de id, paginadas.</summary>
internal sealed record ListarCuentasQuery(Paginacion Paginacion, FiltrosCuentas Filtros);

internal sealed record ObtenerCuentaQuery(int Id);

internal sealed class ListarCuentasValidator : AbstractValidator<ListarCuentasQuery>
{
    public ListarCuentasValidator()
    {
        Include(new PaginacionValidator<ListarCuentasQuery>(q => q.Paginacion));

        RuleFor(q => q.Filtros.RolId).InclusiveBetween((byte)1, (byte)3)
            .OverridePropertyName("rolId").WithName("rol").When(q => q.Filtros.RolId is not null);
        RuleFor(q => q.Filtros.AreaAcademicaId).GreaterThan(0)
            .OverridePropertyName("areaAcademicaId").WithName("área académica").When(q => q.Filtros.AreaAcademicaId is not null);
        RuleFor(q => q.Filtros.EntidadAcademicaId).GreaterThan(0)
            .OverridePropertyName("entidadAcademicaId").WithName("entidad académica").When(q => q.Filtros.EntidadAcademicaId is not null);
        RuleFor(q => q.Filtros.Busqueda).MaximumLength(200)
            .OverridePropertyName("busqueda").WithName("búsqueda").When(q => q.Filtros.Busqueda is not null);
    }
}
