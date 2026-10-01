using System.Security.Cryptography;
using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.SolicitudesApertura.Application.Ambito;
using Sgpla.Modules.SolicitudesApertura.Application.Periodos;
using Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;
using Sgpla.SharedKernel;

namespace Sgpla.UnitTests.SolicitudesApertura;

internal sealed class SolicitudAperturaRepositoryFalso : ISolicitudAperturaRepository
{
    private readonly Dictionary<int, SolicitudApertura> _porId = [];

    public List<SolicitudApertura> Agregadas { get; } = [];

    public List<ArchivoSolicitudApertura> OficiosEliminados { get; } = [];

    public bool TienePendiente { get; set; }

    public Task<SolicitudApertura?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(_porId.GetValueOrDefault(id));

    public Task<bool> ExistePendienteAsync(
        int experienciaEducativaId,
        int periodoEscolarId,
        string seccion,
        CancellationToken cancellationToken) => Task.FromResult(TienePendiente);

    public void Precargar(int id, SolicitudApertura solicitud) => _porId[id] = solicitud;

    public void Agregar(SolicitudApertura solicitud) => Agregadas.Add(solicitud);

    public void EliminarOficio(ArchivoSolicitudApertura oficio) => OficiosEliminados.Add(oficio);
}

internal sealed class ExperienciasEducativasFalsas : IExperienciasEducativas
{
    public Dictionary<int, ExperienciaEducativaResumen> Resumenes { get; } = [];

    public int Consultas { get; private set; }

    public Task<IReadOnlyDictionary<int, ExperienciaEducativaResumen>> ObtenerAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        Consultas++;
        return Task.FromResult<IReadOnlyDictionary<int, ExperienciaEducativaResumen>>(
            Resumenes.Where(par => ids.Contains(par.Key)).ToDictionary(par => par.Key, par => par.Value));
    }

    public Task<IQueryable<int>> ConsultarIdsVisiblesAsync(int? entidadAcademicaId, CancellationToken cancellationToken) =>
        Task.FromResult(Resumenes.Values
            .Where(e => entidadAcademicaId is null || e.EntidadAcademicaId == entidadAcademicaId)
            .Select(e => e.Id)
            .AsQueryable());
}

/// <summary>Ámbito en memoria: solo las EE registradas están dentro del ámbito de cada rol.</summary>
internal sealed class AmbitoSolicitudesAperturaFalso : IAmbitoSolicitudesApertura
{
    public Dictionary<int, ExperienciaEducativaResumen> DeSuEntidad { get; } = [];

    public Dictionary<int, ExperienciaEducativaResumen> DeSuArea { get; } = [];

    public Task<ExperienciaEducativaResumen?> ExperienciaDeSuEntidadAsync(
        int experienciaEducativaId,
        CancellationToken cancellationToken) =>
        Task.FromResult(DeSuEntidad.GetValueOrDefault(experienciaEducativaId));

    public Task<ExperienciaEducativaResumen?> ExperienciaDeSuAreaAsync(
        int experienciaEducativaId,
        CancellationToken cancellationToken) =>
        Task.FromResult(DeSuArea.GetValueOrDefault(experienciaEducativaId));
}

internal sealed class PeriodosEscolaresFalsos : IPeriodosEscolares
{
    public Dictionary<int, PeriodoEscolarResumen> PorId { get; } = [];

    public Dictionary<string, PeriodoEscolarResumen> PorClave { get; } = new(StringComparer.Ordinal);

    public Task<IReadOnlyDictionary<int, PeriodoEscolarResumen>> ObtenerAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<int, PeriodoEscolarResumen>>(
            PorId.Where(par => ids.Contains(par.Key)).ToDictionary(par => par.Key, par => par.Value));

    public Task<IReadOnlyDictionary<string, PeriodoEscolarResumen>> ObtenerActivosPorClaveAsync(
        IReadOnlyCollection<string> claves,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<string, PeriodoEscolarResumen>>(
            PorClave.Where(par => claves.Contains(par.Key, StringComparer.Ordinal))
                .ToDictionary(par => par.Key, par => par.Value, StringComparer.Ordinal));
}

internal sealed class PeriodosConfiguradosFalsos : IPeriodosConfigurados
{
    public string ClaveActual { get; init; } = "202601";

    public string ClaveSiguiente { get; init; } = "202651";
}

internal sealed class AlmacenamientoArchivosFalso : IAlmacenamientoArchivos
{
    private readonly Dictionary<string, byte[]> _archivos = new(StringComparer.Ordinal);

    public long TamanoMaximoBytes { get; set; } = 1024;

    public int Guardados { get; private set; }

    public List<string> Eliminados { get; } = [];

    public async Task<ArchivoGuardado> GuardarAsync(
        string carpeta,
        string extension,
        Stream contenido,
        CancellationToken cancellationToken)
    {
        using var memoria = new MemoryStream();
        await contenido.CopyToAsync(memoria, cancellationToken);
        var bytes = memoria.ToArray();
        var clave = $"{carpeta}/{Guid.NewGuid():N}{extension}";
        _archivos[clave] = bytes;
        Guardados++;
        return new ArchivoGuardado(clave, bytes.LongLength, SHA256.HashData(bytes));
    }

    public Task<Stream?> AbrirAsync(string clave, CancellationToken cancellationToken) =>
        Task.FromResult<Stream?>(_archivos.TryGetValue(clave, out var bytes) ? new MemoryStream(bytes, writable: false) : null);

    public Task<bool> IntentarEliminarAsync(string clave, CancellationToken cancellationToken)
    {
        Eliminados.Add(clave);
        _archivos.Remove(clave);
        return Task.FromResult(true);
    }
}

internal sealed class UsuarioActualFalso(int id, Rol rol, int? entidadAcademicaId, int? areaAcademicaId = null) : ICurrentUser
{
    public int Id => id;

    public Rol Rol => rol;

    public int? AreaAcademicaId => areaAcademicaId;

    public int? EntidadAcademicaId => entidadAcademicaId;
}

internal sealed class UnitOfWorkFalso : IUnitOfWork
{
    public Exception? Excepcion { get; set; }

    public int Guardados { get; private set; }

    /// <summary>Se invoca dentro de <see cref="SaveChangesAsync"/>, antes de lanzar la excepción o de terminar.</summary>
    public Action? AlGuardar { get; set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        Guardados++;
        AlGuardar?.Invoke();
        return Excepcion is null ? Task.CompletedTask : Task.FromException(Excepcion);
    }
}

internal sealed class RelojFalso(DateTimeOffset ahora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => ahora;
}
