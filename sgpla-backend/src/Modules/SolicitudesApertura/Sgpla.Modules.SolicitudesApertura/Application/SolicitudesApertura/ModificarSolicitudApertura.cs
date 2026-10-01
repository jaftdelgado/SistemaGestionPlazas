using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.SolicitudesApertura.Application.Ambito;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;

internal sealed record ModificarSolicitudAperturaCommand(
    int Id,
    int CantidadEstudiantes,
    string? Justificacion,
    ArchivoRecibido? Oficio);

internal sealed class ModificarSolicitudAperturaHandler(
    ISolicitudAperturaRepository repositorio,
    IAmbitoSolicitudesApertura ambito,
    IAlmacenamientoArchivos almacenamiento,
    ICurrentUser actual,
    IUnitOfWork unidadDeTrabajo,
    TimeProvider reloj) : ICommandHandler<ModificarSolicitudAperturaCommand>
{
    public async Task<Result> HandleAsync(ModificarSolicitudAperturaCommand command, CancellationToken cancellationToken)
    {
        var solicitud = await repositorio.ObtenerPorIdAsync(command.Id, cancellationToken);
        var experiencia = solicitud is null
            ? null
            : await ambito.ExperienciaDeSuEntidadAsync(solicitud.ExperienciaEducativaId, cancellationToken);
        if (solicitud is null || experiencia is null)
        {
            return SolicitudAperturaErrors.NoEncontrada(command.Id);
        }

        string? nombreOficio = null;
        if (command.Oficio is { } oficio)
        {
            var validacionOficio = ArchivoSolicitudApertura.ValidarOficio(
                oficio.Nombre, oficio.TipoContenido, oficio.Tamano, almacenamiento.TamanoMaximoBytes);
            if (validacionOficio.IsFailure)
            {
                return validacionOficio;
            }

            if (!ArchivoSolicitudApertura.TieneFirmaPdf(
                    await oficio.LeerEncabezadoAsync(ArchivoSolicitudApertura.LongitudFirmaPdf, cancellationToken)))
            {
                return ArchivoSolicitudAperturaErrors.NoEsPdf;
            }

            nombreOficio = validacionOficio.Value;
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var instante = ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond));
        var modificada = solicitud.Modificar(
            command.CantidadEstudiantes,
            command.Justificacion,
            experiencia.CupoMinimo,
            experiencia.CupoMaximo,
            actual.Id,
            instante);
        if (modificada.IsFailure)
        {
            return modificada;
        }

        ArchivoGuardado? guardado = null;
        ArchivoSolicitudApertura? anterior = null;
        if (command.Oficio is { } nuevoOficio)
        {
            await using var contenido = nuevoOficio.AbrirLectura();
            guardado = await almacenamiento.GuardarAsync("solicitudes-apertura", ".pdf", contenido, cancellationToken);
            var archivo = ArchivoSolicitudApertura.Crear(
                nombreOficio!,
                guardado.Tamano,
                guardado.ChecksumSha256.ToArray(),
                guardado.Clave,
                actual.Id,
                instante);
            anterior = solicitud.ReemplazarOficio(archivo);
            repositorio.EliminarOficio(anterior);
        }

        try
        {
            await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (guardado is not null)
            {
                await almacenamiento.IntentarEliminarAsync(guardado.Clave, CancellationToken.None);
            }

            throw;
        }

        if (anterior is not null)
        {
            await almacenamiento.IntentarEliminarAsync(anterior.ClaveAlmacenamiento, CancellationToken.None);
        }

        return Result.Success();
    }
}
