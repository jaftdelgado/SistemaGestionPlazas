using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.Usuarios.Application.Sesion;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Usuarios.Infrastructure.Sesion;

/// <summary>La usan iniciar sesión, cambiar contraseña y <c>GET /sesion</c>, para no repetir cómo se arma la respuesta.</summary>
internal sealed class ObtenerUsuarioSesionHandler(SgplaDbContext contexto, IAmbitosInstitucionales ambitos)
    : IQueryHandler<ObtenerUsuarioSesionQuery, UsuarioSesionResponse>
{
    public async Task<Result<UsuarioSesionResponse>> HandleAsync(
        ObtenerUsuarioSesionQuery query, CancellationToken cancellationToken)
    {
        var cuenta = await contexto.Set<Usuario>()
            .AsNoTracking()
            .Where(u => u.Id == query.UsuarioId)
            .Select(u => new
            {
                u.Id,
                u.Correo,
                u.Nombre,
                u.Rol,
                AreaAcademicaId = u.PerfilDgaa != null ? (int?)u.PerfilDgaa.AreaAcademicaId : null,
                EntidadAcademicaId = u.PerfilEntidadAcademica != null ? (int?)u.PerfilEntidadAcademica.EntidadAcademicaId : null,
                CambioContrasenaPendiente = u.Credencial != null && u.Credencial.FechaActualizacion == null,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (cuenta is null)
        {
            return UsuarioErrors.NoEncontrado(query.UsuarioId);
        }

        AreaAcademicaResumenResponse? area = null;
        EntidadAcademicaResumenResponse? entidad = null;

        if (cuenta.AreaAcademicaId is { } areaId)
        {
            var areas = await ambitos.ObtenerAreasAsync([areaId], cancellationToken);
            if (areas.TryGetValue(areaId, out var resumen))
            {
                area = new AreaAcademicaResumenResponse(resumen.Id, resumen.Clave, resumen.Nombre);
            }
        }
        else if (cuenta.EntidadAcademicaId is { } entidadId)
        {
            var entidades = await ambitos.ObtenerEntidadesAsync([entidadId], cancellationToken);
            if (entidades.TryGetValue(entidadId, out var resumen))
            {
                entidad = new EntidadAcademicaResumenResponse(resumen.Id, resumen.Clave, resumen.Nombre);
            }
        }

        return new UsuarioSesionResponse(
            cuenta.Id,
            cuenta.Correo,
            cuenta.Nombre,
            new RolResponse((byte)cuenta.Rol, NombresRol.Nombre(cuenta.Rol)),
            area,
            entidad,
            cuenta.CambioContrasenaPendiente);
    }
}
