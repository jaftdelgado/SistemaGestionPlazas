using System.Globalization;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;

internal static class SolicitudAperturaErrors
{
    public static readonly Error SeccionVacia = Error.Validation(
        "SolicitudApertura.SeccionVacia", "La sección es obligatoria.", nameof(SolicitudApertura.Seccion));

    public static readonly Error SeccionDemasiadoLarga = Error.Validation(
        "SolicitudApertura.SeccionDemasiadoLarga",
        $"La sección admite hasta {SolicitudApertura.LongitudMaximaSeccion} caracteres.",
        nameof(SolicitudApertura.Seccion));

    public static readonly Error SeccionFormatoInvalido = Error.Validation(
        "SolicitudApertura.SeccionFormatoInvalido",
        "La sección solo admite letras sin acentos y dígitos, sin espacios.",
        nameof(SolicitudApertura.Seccion));

    public static readonly Error CantidadNoPositiva = Error.Validation(
        "SolicitudApertura.CantidadNoPositiva",
        "La cantidad de estudiantes debe ser mayor que cero.",
        nameof(SolicitudApertura.CantidadEstudiantes));

    public static readonly Error JustificacionVacia = Error.Validation(
        "SolicitudApertura.JustificacionVacia", "La justificación es obligatoria.", nameof(SolicitudApertura.Justificacion));

    public static readonly Error JustificacionDemasiadoLarga = Error.Validation(
        "SolicitudApertura.JustificacionDemasiadoLarga",
        $"La justificación admite hasta {SolicitudApertura.LongitudMaximaJustificacion} caracteres.",
        nameof(SolicitudApertura.Justificacion));

    public static readonly Error ExperienciaEducativaInvalida = Error.Validation(
        "SolicitudApertura.ExperienciaEducativaInvalida",
        "La experiencia educativa no existe, está dada de baja o no pertenece a tu entidad académica.",
        nameof(SolicitudApertura.ExperienciaEducativaId));

    public static readonly Error PeriodoEscolarInvalido = Error.Validation(
        "SolicitudApertura.PeriodoEscolarInvalido",
        "El periodo escolar no existe o está dado de baja.",
        nameof(SolicitudApertura.PeriodoEscolarId));

    public static readonly Error PeriodosNoDisponibles = Error.Conflict(
        "SolicitudApertura.PeriodosNoDisponibles",
        "El periodo actual o el siguiente configurados no existen o están dados de baja.");

    public static readonly Error PeriodoNoAbierto = Error.Conflict(
        "SolicitudApertura.PeriodoNoAbierto", "Solo se reciben solicitudes para el periodo siguiente configurado.");

    public static readonly Error CantidadFueraDeCupos = Error.Conflict(
        "SolicitudApertura.CantidadFueraDeCupos",
        "La cantidad de estudiantes está fuera de los cupos de la experiencia educativa.");

    public static readonly Error SeccionDuplicada = Error.Conflict(
        "SolicitudApertura.SeccionDuplicada",
        "Ya existe una solicitud pendiente de esa sección para la experiencia educativa y el periodo.");

    public static readonly Error ComentariosVacios = Error.Validation(
        "SolicitudApertura.ComentariosVacios", "Los comentarios son obligatorios para rechazar.", "Comentarios");

    public static readonly Error ComentariosDemasiadoLargos = Error.Validation(
        "SolicitudApertura.ComentariosDemasiadoLargos",
        $"Los comentarios admiten hasta {SolicitudApertura.LongitudMaximaComentarios} caracteres.",
        "Comentarios");

    public static readonly Error MotivoVacio = Error.Validation(
        "SolicitudApertura.MotivoVacio", "El motivo es obligatorio.", "Motivo");

    public static readonly Error MotivoDemasiadoLargo = Error.Validation(
        "SolicitudApertura.MotivoDemasiadoLargo",
        $"El motivo admite hasta {SolicitudApertura.LongitudMaximaMotivo} caracteres.",
        "Motivo");

    public static readonly Error NoPendiente = Error.Conflict(
        "SolicitudApertura.NoPendiente", "La solicitud ya no está pendiente.");

    public static readonly Error CuposIncompletos = Error.Conflict(
        "SolicitudApertura.CuposIncompletos",
        "La experiencia educativa necesita cupo mínimo y máximo para aceptar la solicitud.");

    public static Error NoEncontrada(int id) => Error.NotFound(
        "SolicitudApertura.NoEncontrada", $"No existe la solicitud de apertura {id.ToString(CultureInfo.InvariantCulture)}.");
}
