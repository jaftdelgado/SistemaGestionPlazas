using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.Usuarios.Application.Cuentas;
using Sgpla.Modules.Usuarios.Application.Sesion;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Usuarios.Infrastructure.Cuentas;

internal sealed class ObtenerCuentaHandler(SgplaDbContext contexto, IAmbitosInstitucionales ambitos)
    : IQueryHandler<ObtenerCuentaQuery, CuentaResponse>
{
    public async Task<Result<CuentaResponse>> HandleAsync(ObtenerCuentaQuery query, CancellationToken cancellationToken)
    {
        var cuenta = await CuentaProyeccion.Proyectar(contexto.Set<Usuario>().Where(u => u.Id == query.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (cuenta is null)
        {
            return UsuarioErrors.NoEncontrado(query.Id);
        }

        var areas = cuenta.AreaAcademicaId is { } areaId
            ? await ambitos.ObtenerAreasAsync([areaId], cancellationToken)
            : new Dictionary<int, AreaAcademicaResumen>();
        var entidades = cuenta.EntidadAcademicaId is { } entidadId
            ? await ambitos.ObtenerEntidadesAsync([entidadId], cancellationToken)
            : new Dictionary<int, EntidadAcademicaResumen>();

        return CuentaProyeccion.ArmarRespuesta(cuenta, areas, entidades);
    }
}

internal sealed class ListarCuentasHandler(SgplaDbContext contexto, IAmbitosInstitucionales ambitos)
    : IQueryHandler<ListarCuentasQuery, Pagina<CuentaResponse>>
{
    public async Task<Result<Pagina<CuentaResponse>>> HandleAsync(ListarCuentasQuery query, CancellationToken cancellationToken)
    {
        var filtros = query.Filtros;
        var cuentas = contexto.Set<Usuario>().AsQueryable();

        if (filtros.RolId is not null)
        {
            var rol = (Rol)filtros.RolId.Value;
            cuentas = cuentas.Where(u => u.Rol == rol);
        }

        if (filtros.AreaAcademicaId is not null)
        {
            var area = filtros.AreaAcademicaId.Value;
            var entidadesDelArea = (await ambitos.ObtenerEntidadesDeAreaAsync(area, cancellationToken)).ToList();
            cuentas = cuentas.Where(u =>
                u.PerfilDgaa!.AreaAcademicaId == area
                || entidadesDelArea.Contains(u.PerfilEntidadAcademica!.EntidadAcademicaId));
        }

        if (filtros.EntidadAcademicaId is not null)
        {
            var entidad = filtros.EntidadAcademicaId.Value;
            cuentas = cuentas.Where(u => u.PerfilEntidadAcademica!.EntidadAcademicaId == entidad);
        }

        var busqueda = NormalizarTexto(filtros.Busqueda);
        if (busqueda is not null)
        {
            cuentas = cuentas.Where(u =>
                u.Correo.Contains(busqueda)
                || EF.Functions.Collate(u.Nombre, "Modern_Spanish_100_CI_AI").Contains(busqueda));
        }

        var proyeccion = CuentaProyeccion.Proyectar(cuentas.OrderBy(u => u.Id));
        var pagina = await proyeccion.PaginarAsync(query.Paginacion, cancellationToken);

        var areaIds = pagina.Elementos.Where(x => x.AreaAcademicaId is not null)
            .Select(x => x.AreaAcademicaId!.Value).Distinct().ToList();
        var entidadIds = pagina.Elementos.Where(x => x.EntidadAcademicaId is not null)
            .Select(x => x.EntidadAcademicaId!.Value).Distinct().ToList();

        var areas = await ambitos.ObtenerAreasAsync(areaIds, cancellationToken);
        var entidades = await ambitos.ObtenerEntidadesAsync(entidadIds, cancellationToken);

        var elementos = pagina.Elementos.Select(x => CuentaProyeccion.ArmarRespuesta(x, areas, entidades)).ToList();
        return new Pagina<CuentaResponse>(elementos, pagina.NumeroPagina, pagina.TamanoPagina, pagina.Total);
    }

    private static string? NormalizarTexto(string? valor)
    {
        if (valor is null)
        {
            return null;
        }

        var normalizado = Normalizacion.Texto(valor);
        return normalizado.Length == 0 ? null : normalizado;
    }
}

internal sealed record CuentaProyeccionIntermedia(
    int Id, string Correo, string Nombre, Rol Rol, int? AreaAcademicaId, int? EntidadAcademicaId);

/// <summary>Proyección compartida entre <see cref="ObtenerCuentaHandler"/> y <see cref="ListarCuentasHandler"/>.</summary>
internal static class CuentaProyeccion
{
    public static IQueryable<CuentaProyeccionIntermedia> Proyectar(IQueryable<Usuario> cuentas) =>
        cuentas.AsNoTracking().Select(u => new CuentaProyeccionIntermedia(
            u.Id,
            u.Correo,
            u.Nombre,
            u.Rol,
            u.PerfilDgaa != null ? (int?)u.PerfilDgaa.AreaAcademicaId : null,
            u.PerfilEntidadAcademica != null ? (int?)u.PerfilEntidadAcademica.EntidadAcademicaId : null));

    public static CuentaResponse ArmarRespuesta(
        CuentaProyeccionIntermedia cuenta,
        IReadOnlyDictionary<int, AreaAcademicaResumen> areas,
        IReadOnlyDictionary<int, EntidadAcademicaResumen> entidades)
    {
        AreaAcademicaResumenResponse? area = null;
        if (cuenta.AreaAcademicaId is { } areaId && areas.TryGetValue(areaId, out var resumenArea))
        {
            area = new AreaAcademicaResumenResponse(resumenArea.Id, resumenArea.Clave, resumenArea.Nombre);
        }

        EntidadAcademicaResumenResponse? entidad = null;
        if (cuenta.EntidadAcademicaId is { } entidadId && entidades.TryGetValue(entidadId, out var resumenEntidad))
        {
            entidad = new EntidadAcademicaResumenResponse(resumenEntidad.Id, resumenEntidad.Clave, resumenEntidad.Nombre);
        }

        return new CuentaResponse(
            cuenta.Id,
            cuenta.Correo,
            cuenta.Nombre,
            new RolResponse((byte)cuenta.Rol, NombresRol.Nombre(cuenta.Rol)),
            area,
            entidad);
    }
}
