using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.Institucional.Application.EntidadesAcademicas;
using Sgpla.Modules.Institucional.Application.Ubicaciones;
using Sgpla.Modules.Institucional.Domain.AreasAcademicas;
using Sgpla.Modules.Institucional.Domain.EntidadesAcademicas;
using Sgpla.Modules.Institucional.Domain.Ubicaciones;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Institucional.Infrastructure.EntidadesAcademicas;

internal sealed class ObtenerEntidadAcademicaHandler(SgplaDbContext contexto, IMunicipios municipios)
    : IQueryHandler<ObtenerEntidadAcademicaQuery, EntidadAcademicaResponse>
{
    public async Task<Result<EntidadAcademicaResponse>> HandleAsync(
        ObtenerEntidadAcademicaQuery query,
        CancellationToken cancellationToken)
    {
        var encontrada = await EntidadAcademicaProyeccion
            .Proyectar(contexto, contexto.Set<EntidadAcademica>().Where(e => e.Id == query.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (encontrada is null)
        {
            return EntidadAcademicaErrors.NoEncontrado(query.Id);
        }

        var nombres = await municipios.ObtenerNombresAsync([encontrada.MunicipioId], cancellationToken);
        return encontrada.ArmarRespuesta(nombres);
    }
}

internal sealed class ListarEntidadesAcademicasHandler(SgplaDbContext contexto, IMunicipios municipios)
    : IQueryHandler<ListarEntidadesAcademicasQuery, Pagina<EntidadAcademicaResponse>>
{
    public async Task<Result<Pagina<EntidadAcademicaResponse>>> HandleAsync(
        ListarEntidadesAcademicasQuery query,
        CancellationToken cancellationToken)
    {
        var filtros = query.Filtros;
        var entidades = contexto.Set<EntidadAcademica>().AsQueryable();

        if (filtros.CampusId is not null)
        {
            entidades = entidades.Where(e => e.CampusId == filtros.CampusId);
        }

        if (filtros.RegionId is not null)
        {
            var campusDeLaRegion = contexto.Set<Campus>()
                .Where(c => c.RegionId == filtros.RegionId)
                .Select(c => c.Id);
            entidades = entidades.Where(e => campusDeLaRegion.Contains(e.CampusId));
        }

        if (filtros.AreaAcademicaId is not null)
        {
            entidades = entidades.Where(e => e.AreaAcademicaId == filtros.AreaAcademicaId);
        }

        if (filtros.MunicipioId is not null)
        {
            entidades = entidades.Where(e => e.MunicipioId == filtros.MunicipioId);
        }

        var busqueda = NormalizarTexto(filtros.Busqueda);
        if (busqueda is not null)
        {
            var busquedaClave = busqueda.ToUpperInvariant();
            entidades = entidades.Where(e =>
                e.Clave.Contains(busquedaClave)
                || EF.Functions.Collate(e.Nombre, "Modern_Spanish_100_CI_AI").Contains(busqueda));
        }

        var calle = NormalizarTexto(filtros.Calle);
        if (calle is not null)
        {
            entidades = entidades.Where(e => e.Calle.Contains(calle));
        }

        var colonia = NormalizarTexto(filtros.Colonia);
        if (colonia is not null)
        {
            entidades = entidades.Where(e => e.Colonia.Contains(colonia));
        }

        var numeroExterior = NormalizarTexto(filtros.NumeroExterior);
        if (numeroExterior is not null)
        {
            entidades = entidades.Where(e => e.NumeroExterior != null && e.NumeroExterior.Contains(numeroExterior));
        }

        var codigoPostal = Recortar(filtros.CodigoPostal);
        if (codigoPostal is not null)
        {
            entidades = entidades.Where(e => e.CodigoPostal == codigoPostal);
        }

        var telefono = Recortar(filtros.Telefono);
        if (telefono is not null)
        {
            entidades = entidades.Where(e => e.Telefono == telefono);
        }

        var proyeccion = EntidadAcademicaProyeccion.ConOrdenYRelaciones(contexto, entidades);

        var pagina = await proyeccion.PaginarAsync(query.Paginacion, cancellationToken);

        var municipioIds = pagina.Elementos.Select(x => x.MunicipioId).Distinct().ToList();
        var nombres = await municipios.ObtenerNombresAsync(municipioIds, cancellationToken);
        var elementos = pagina.Elementos.Select(x => x.ArmarRespuesta(nombres)).ToList();

        return new Pagina<EntidadAcademicaResponse>(elementos, pagina.NumeroPagina, pagina.TamanoPagina, pagina.Total);
    }

    private static string? NormalizarTexto(string? valor) => NuloSiVacio(valor is null ? null : Normalizacion.Texto(valor));

    private static string? Recortar(string? valor) => NuloSiVacio(valor is null ? null : Normalizacion.Recortar(valor));

    private static string? NuloSiVacio(string? valor) => string.IsNullOrEmpty(valor) ? null : valor;
}

internal sealed record EntidadAcademicaProyeccionIntermedia(
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
    int MunicipioId)
{
    public EntidadAcademicaResponse ArmarRespuesta(IReadOnlyDictionary<int, string> nombresMunicipios) => new(
        Id, Clave, Nombre, Calle, NumeroExterior, Colonia, CodigoPostal, Telefono, Extension, Campus, AreaAcademica,
        new ReferenciaResponse(MunicipioId, nombresMunicipios[MunicipioId]));
}

internal static class EntidadAcademicaProyeccion
{
    /// <summary>Une cada entidad con su campus, región y área, sin ordenar: para obtener una sola por id.</summary>
    public static IQueryable<EntidadAcademicaProyeccionIntermedia> Proyectar(
        SgplaDbContext contexto, IQueryable<EntidadAcademica> entidades) =>
        from e in entidades.AsNoTracking()
        join c in contexto.Set<Campus>().AsNoTracking() on e.CampusId equals c.Id
        join r in contexto.Set<Region>().AsNoTracking() on c.RegionId equals r.Id
        join a in contexto.Set<AreaAcademica>().AsNoTracking() on e.AreaAcademicaId equals a.Id
        select new EntidadAcademicaProyeccionIntermedia(
            e.Id,
            e.Clave,
            e.Nombre,
            e.Calle,
            e.NumeroExterior,
            e.Colonia,
            e.CodigoPostal,
            e.Telefono,
            e.Extension,
            new CampusResponse(c.Id, c.Clave, c.Nombre, new RegionResponse(r.Id, r.Clave, r.Nombre)),
            new AreaAcademicaResumenResponse(a.Id, a.Clave, a.Nombre),
            e.MunicipioId);

    /// <summary>Igual que <see cref="Proyectar"/>, pero en orden de clave y luego id: para el listado paginado. El
    /// orden se aplica antes de proyectar al record, porque EF Core no puede traducirlo después.</summary>
    public static IQueryable<EntidadAcademicaProyeccionIntermedia> ConOrdenYRelaciones(
        SgplaDbContext contexto, IQueryable<EntidadAcademica> entidades) =>
        from e in entidades.AsNoTracking()
        join c in contexto.Set<Campus>().AsNoTracking() on e.CampusId equals c.Id
        join r in contexto.Set<Region>().AsNoTracking() on c.RegionId equals r.Id
        join a in contexto.Set<AreaAcademica>().AsNoTracking() on e.AreaAcademicaId equals a.Id
        orderby e.Clave, e.Id
        select new EntidadAcademicaProyeccionIntermedia(
            e.Id,
            e.Clave,
            e.Nombre,
            e.Calle,
            e.NumeroExterior,
            e.Colonia,
            e.CodigoPostal,
            e.Telefono,
            e.Extension,
            new CampusResponse(c.Id, c.Clave, c.Nombre, new RegionResponse(r.Id, r.Clave, r.Nombre)),
            new AreaAcademicaResumenResponse(a.Id, a.Clave, a.Nombre),
            e.MunicipioId);
}
