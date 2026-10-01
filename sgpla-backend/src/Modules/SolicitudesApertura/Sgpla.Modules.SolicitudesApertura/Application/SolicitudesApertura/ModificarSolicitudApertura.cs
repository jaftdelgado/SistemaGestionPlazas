using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
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
    IExperienciasEducativas experienciasEducativas,
    IAlmacenamientoArchivos almacenamiento,
    ICurrentUser actual,
    IUnitOfWork unidadDeTrabajo,
    TimeProvider reloj) : ICommandHandler<ModificarSolicitudAperturaCommand>
{
    public async Task<Result> HandleAsync(ModificarSolicitudAperturaCommand command, CancellationToken cancellationToken)
    {
        var cargada = await SolicitudAperturaComandos.ObtenerEnAmbitoAsync(
            repositorio,
            experienciasEducativas,
            command.Id,
            e => e.EntidadAcademicaId == actual.EntidadAcademicaId,
            cancellationToken);
        if (cargada.IsFailure)
        {
            return cargada;
        }

        var (solicitud, experiencia) = cargada.Value;

        string? nombreOficio = null;
        if (command.Oficio is { } oficio)
        {
            var validacionOficio = await SolicitudAperturaComandos.ValidarOficioAsync(
                oficio, almacenamiento.TamanoMaximoBytes, cancellationToken);
            if (validacionOficio.IsFailure)
            {
                return validacionOficio;
            }

            nombreOficio = validacionOficio.Value;
        }

        var utc = SolicitudAperturaComandos.Ahora(reloj);
        var modificada = solicitud.Modificar(
            command.CantidadEstudiantes,
            command.Justificacion,
            experiencia.CupoMinimo,
            experiencia.CupoMaximo,
            actual.Id,
            utc);
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
                utc);
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
