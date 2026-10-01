using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;

/// <summary>Una solicitud cargada junto con el resumen de su experiencia educativa, ya dentro del ámbito del usuario.</summary>
internal sealed record SolicitudEnAmbito(SolicitudApertura Solicitud, ExperienciaEducativaResumen Experiencia);

/// <summary>Pasos que comparten los comandos sobre una solicitud.</summary>
internal static class SolicitudAperturaComandos
{
    private const int LongitudFirmaPdf = 5;

    /// <summary>
    /// Carga la solicitud y su experiencia educativa. Una solicitud inexistente o fuera del ámbito responde lo mismo,
    /// sin revelar si existe.
    /// </summary>
    public static async Task<Result<SolicitudEnAmbito>> ObtenerEnAmbitoAsync(
        ISolicitudAperturaRepository repositorio,
        IExperienciasEducativas experienciasEducativas,
        int id,
        Func<ExperienciaEducativaResumen, bool> enAmbito,
        CancellationToken cancellationToken)
    {
        var solicitud = await repositorio.ObtenerPorIdAsync(id, cancellationToken);
        if (solicitud is null)
        {
            return SolicitudAperturaErrors.NoEncontrada(id);
        }

        var experienciaPorId = await experienciasEducativas.ObtenerAsync([solicitud.ExperienciaEducativaId], cancellationToken);
        if (!experienciaPorId.TryGetValue(solicitud.ExperienciaEducativaId, out var experiencia) || !enAmbito(experiencia))
        {
            return SolicitudAperturaErrors.NoEncontrada(id);
        }

        return new SolicitudEnAmbito(solicitud, experiencia);
    }

    /// <summary>Valida el oficio recibido y su firma <c>%PDF-</c>; devuelve el nombre normalizado.</summary>
    public static async Task<Result<string>> ValidarOficioAsync(
        ArchivoRecibido oficio,
        long tamanoMaximoBytes,
        CancellationToken cancellationToken)
    {
        var validacion = ArchivoSolicitudApertura.ValidarOficio(
            oficio.Nombre, oficio.TipoContenido, oficio.Tamano, tamanoMaximoBytes);
        if (validacion.IsFailure)
        {
            return validacion;
        }

        await using var stream = oficio.AbrirLectura();
        var encabezado = new byte[LongitudFirmaPdf];
        var leidos = 0;
        while (leidos < encabezado.Length)
        {
            var cantidad = await stream.ReadAsync(encabezado.AsMemory(leidos), cancellationToken);
            if (cantidad == 0)
            {
                break;
            }

            leidos += cantidad;
        }

        return ArchivoSolicitudApertura.TieneFirmaPdf(encabezado.AsSpan(0, leidos))
            ? validacion
            : ArchivoSolicitudAperturaErrors.NoEsPdf;
    }

    /// <summary>Instante actual en UTC, truncado a segundos, que es la precisión de las columnas.</summary>
    public static DateTime Ahora(TimeProvider reloj)
    {
        var utc = reloj.GetUtcNow().UtcDateTime;
        return new DateTime(utc.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond, DateTimeKind.Utc);
    }
}
