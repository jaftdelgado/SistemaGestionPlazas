using Sgpla.Modules.Institucional.Domain.AreasAcademicas;

namespace Sgpla.Modules.Institucional.Application.AreasAcademicas;

internal sealed record AreaAcademicaResponse(int Id, int Clave, string Nombre, string Telefono, string? Extension)
{
    public static AreaAcademicaResponse Desde(AreaAcademica area) =>
        new(area.Id, area.Clave, area.Nombre, area.Telefono, area.Extension);
}

/// <summary>Todas las áreas activas, en orden de clave.</summary>
internal sealed record ListarAreasAcademicasQuery;

internal sealed record ObtenerAreaAcademicaQuery(int Id);
