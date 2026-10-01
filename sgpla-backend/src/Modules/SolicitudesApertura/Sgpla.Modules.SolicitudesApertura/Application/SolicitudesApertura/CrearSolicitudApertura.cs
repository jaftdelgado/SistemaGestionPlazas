using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.SolicitudesApertura.Application.Periodos;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;

internal sealed record CrearSolicitudAperturaCommand(
    int ExperienciaEducativaId,
    int PeriodoEscolarId,
    string? Seccion,
    int CantidadEstudiantes,
    string? Justificacion,
    ArchivoRecibido? Oficio);

internal sealed class CrearSolicitudAperturaHandler(
    ISolicitudAperturaRepository repositorio,
    IExperienciasEducativas experienciasEducativas,
    IPeriodosEscolares periodosEscolares,
    IPeriodosConfigurados periodosConfigurados,
    IAlmacenamientoArchivos almacenamiento,
    ICurrentUser actual,
    IUnitOfWork unidadDeTrabajo,
    TimeProvider reloj) : ICommandHandler<CrearSolicitudAperturaCommand, SolicitudAperturaResponse>
{
    public async Task<Result<SolicitudAperturaResponse>> HandleAsync(
        CrearSolicitudAperturaCommand command,
        CancellationToken cancellationToken)
    {
        var datos = DatosSolicitudApertura.Crear(command.Seccion, command.CantidadEstudiantes, command.Justificacion);
        if (datos.IsFailure)
        {
            return datos.Error;
        }

        if (command.Oficio is not { } oficio)
        {
            return ArchivoSolicitudAperturaErrors.Obligatorio;
        }

        var validacionOficio = await SolicitudAperturaComandos.ValidarOficioAsync(
            oficio, almacenamiento.TamanoMaximoBytes, cancellationToken);
        if (validacionOficio.IsFailure)
        {
            return validacionOficio.Error;
        }

        var experienciaPorId = await experienciasEducativas.ObtenerAsync([command.ExperienciaEducativaId], cancellationToken);
        if (!experienciaPorId.TryGetValue(command.ExperienciaEducativaId, out var experiencia)
            || !experiencia.Vigente
            || actual.EntidadAcademicaId != experiencia.EntidadAcademicaId)
        {
            return SolicitudAperturaErrors.ExperienciaEducativaInvalida;
        }

        var periodoPorId = await periodosEscolares.ObtenerAsync([command.PeriodoEscolarId], cancellationToken);
        if (!periodoPorId.TryGetValue(command.PeriodoEscolarId, out var periodo) || !periodo.Activo)
        {
            return SolicitudAperturaErrors.PeriodoEscolarInvalido;
        }

        var periodosActivos = await periodosEscolares.ObtenerActivosPorClaveAsync(
            [periodosConfigurados.ClaveActual, periodosConfigurados.ClaveSiguiente], cancellationToken);
        if (!periodosActivos.ContainsKey(periodosConfigurados.ClaveActual)
            || !periodosActivos.ContainsKey(periodosConfigurados.ClaveSiguiente))
        {
            return SolicitudAperturaErrors.PeriodosNoDisponibles;
        }

        if (!string.Equals(periodo.Clave, periodosConfigurados.ClaveSiguiente, StringComparison.Ordinal))
        {
            return SolicitudAperturaErrors.PeriodoNoAbierto;
        }

        var cupos = SolicitudApertura.ValidarCupos(
            datos.Value.CantidadEstudiantes, experiencia.CupoMinimo, experiencia.CupoMaximo);
        if (cupos.IsFailure)
        {
            return cupos.Error;
        }

        if (await repositorio.ExistePendienteAsync(
                experiencia.Id, periodo.Id, datos.Value.Seccion, cancellationToken))
        {
            return SolicitudAperturaErrors.SeccionDuplicada;
        }

        await using var contenido = oficio.AbrirLectura();
        var guardado = await almacenamiento.GuardarAsync("solicitudes-apertura", ".pdf", contenido, cancellationToken);
        var utc = SolicitudAperturaComandos.Ahora(reloj);
        var archivo = ArchivoSolicitudApertura.Crear(
            validacionOficio.Value,
            guardado.Tamano,
            guardado.ChecksumSha256.ToArray(),
            guardado.Clave,
            actual.Id,
            utc);
        var solicitud = SolicitudApertura.Crear(datos.Value, experiencia.Id, periodo.Id, archivo, actual.Id, utc);
        repositorio.Agregar(solicitud);

        try
        {
            await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await almacenamiento.IntentarEliminarAsync(guardado.Clave, CancellationToken.None);
            throw;
        }

        return CrearResponse(solicitud, experiencia, periodo);
    }

    private static SolicitudAperturaResponse CrearResponse(
        SolicitudApertura solicitud,
        ExperienciaEducativaResumen experiencia,
        PeriodoEscolarResumen periodo) =>
        new(
            solicitud.Id,
            solicitud.Estado.ComoTexto(),
            solicitud.Seccion,
            solicitud.CantidadEstudiantes,
            solicitud.Justificacion,
            new ExperienciaSolicitadaResponse(
                experiencia.Id,
                experiencia.Materia,
                experiencia.Curso,
                experiencia.Nombre,
                experiencia.CupoMinimo,
                experiencia.CupoMaximo,
                experiencia.PlanEstudiosId,
                experiencia.PlanEstudiosCodigo,
                experiencia.ProgramaEducativoId,
                experiencia.ProgramaEducativoNombre,
                experiencia.EntidadAcademicaId,
                experiencia.EntidadAcademicaClave,
                experiencia.EntidadAcademicaNombre,
                experiencia.SistemaEducativoId,
                experiencia.SistemaEducativoNombre),
            new PeriodoSolicitadoResponse(periodo.Id, periodo.Clave),
            new OficioResponse(solicitud.Oficio.Nombre, solicitud.Oficio.Tamano, solicitud.Oficio.CargadoEn),
            solicitud.CreadaEn,
            solicitud.CreadaPorUsuarioId,
            solicitud.ActualizadaEn,
            solicitud.ActualizadaPorUsuarioId,
            solicitud.ResueltaEn,
            solicitud.ResueltaPorUsuarioId,
            solicitud.ComentariosResolucion,
            solicitud.CanceladaEn,
            solicitud.CanceladaPorUsuarioId,
            solicitud.MotivoCancelacion);
}
