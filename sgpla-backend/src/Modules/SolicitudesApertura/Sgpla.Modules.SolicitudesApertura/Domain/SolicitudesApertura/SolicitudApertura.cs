using Sgpla.SharedKernel;

namespace Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;

internal sealed class SolicitudApertura : Entity
{
    public const int LongitudMaximaSeccion = 20;
    public const int LongitudMaximaJustificacion = 2000;
    public const int LongitudMaximaComentarios = 2000;
    public const int LongitudMaximaMotivo = 1000;

    private SolicitudApertura()
    {
    }

    public int ExperienciaEducativaId { get; private set; }

    public int PeriodoEscolarId { get; private set; }

    public string Seccion { get; private set; } = string.Empty;

    public int CantidadEstudiantes { get; private set; }

    public string Justificacion { get; private set; } = string.Empty;

    public int OficioRespaldoId { get; private set; }

    public ArchivoSolicitudApertura Oficio { get; private set; } = null!;

    public EstadoSolicitudApertura Estado { get; private set; }

    public DateTime CreadaEn { get; private set; }

    public int CreadaPorUsuarioId { get; private set; }

    public DateTime? ActualizadaEn { get; private set; }

    public int? ActualizadaPorUsuarioId { get; private set; }

    public DateTime? ResueltaEn { get; private set; }

    public int? ResueltaPorUsuarioId { get; private set; }

    public string? ComentariosResolucion { get; private set; }

    public DateTime? CanceladaEn { get; private set; }

    public int? CanceladaPorUsuarioId { get; private set; }

    public string? MotivoCancelacion { get; private set; }

    public byte[] Version { get; private set; } = [];

    public static SolicitudApertura Crear(
        DatosSolicitudApertura datos,
        int experienciaEducativaId,
        int periodoEscolarId,
        ArchivoSolicitudApertura oficio,
        int usuarioId,
        DateTime utc) =>
        new()
        {
            ExperienciaEducativaId = experienciaEducativaId,
            PeriodoEscolarId = periodoEscolarId,
            Seccion = datos.Seccion,
            CantidadEstudiantes = datos.CantidadEstudiantes,
            Justificacion = datos.Justificacion,
            Oficio = oficio,
            Estado = EstadoSolicitudApertura.Pendiente,
            CreadaEn = utc,
            CreadaPorUsuarioId = usuarioId,
        };

    public static Result ValidarCupos(int cantidadEstudiantes, int? cupoMinimo, int? cupoMaximo) =>
        cantidadEstudiantes < cupoMinimo || cantidadEstudiantes > cupoMaximo
            ? SolicitudAperturaErrors.CantidadFueraDeCupos
            : Result.Success();

    internal static Result ValidarCantidad(int cantidadEstudiantes) =>
        cantidadEstudiantes <= 0 ? SolicitudAperturaErrors.CantidadNoPositiva : Result.Success();

    internal static Result<string> NormalizarJustificacion(string? justificacion)
    {
        var normalizada = Normalizacion.Recortar(justificacion);
        if (normalizada.Length == 0)
        {
            return SolicitudAperturaErrors.JustificacionVacia;
        }

        if (normalizada.Length > LongitudMaximaJustificacion)
        {
            return SolicitudAperturaErrors.JustificacionDemasiadoLarga;
        }

        return normalizada;
    }

    /// <summary>Recorta los comentarios de una resolución; vacío o <c>null</c> devuelve <c>null</c>.</summary>
    internal static Result<string?> NormalizarComentarios(string? comentarios)
    {
        var normalizados = Normalizacion.Recortar(comentarios);
        if (normalizados.Length > LongitudMaximaComentarios)
        {
            return SolicitudAperturaErrors.ComentariosDemasiadoLargos;
        }

        return Result.Success<string?>(normalizados.Length == 0 ? null : normalizados);
    }

    public Result Modificar(
        int cantidadEstudiantes,
        string? justificacion,
        int? cupoMinimo,
        int? cupoMaximo,
        int usuarioId,
        DateTime utc)
    {
        var cantidad = ValidarCantidad(cantidadEstudiantes);
        if (cantidad.IsFailure)
        {
            return cantidad;
        }

        var justificacionNormalizada = NormalizarJustificacion(justificacion);
        if (justificacionNormalizada.IsFailure)
        {
            return justificacionNormalizada;
        }

        if (Estado != EstadoSolicitudApertura.Pendiente)
        {
            return SolicitudAperturaErrors.NoPendiente;
        }

        var cupos = ValidarCupos(cantidadEstudiantes, cupoMinimo, cupoMaximo);
        if (cupos.IsFailure)
        {
            return cupos;
        }

        CantidadEstudiantes = cantidadEstudiantes;
        Justificacion = justificacionNormalizada.Value;
        ActualizadaEn = utc;
        ActualizadaPorUsuarioId = usuarioId;
        return Result.Success();
    }

    /// <summary>Asigna el oficio nuevo y devuelve el anterior, que quien llama debe borrar.</summary>
    public ArchivoSolicitudApertura ReemplazarOficio(ArchivoSolicitudApertura nuevo)
    {
        var anterior = Oficio;
        Oficio = nuevo;
        return anterior;
    }

    public Result Aceptar(string? comentarios, int? cupoMinimo, int? cupoMaximo, int usuarioId, DateTime utc)
    {
        var comentariosNormalizados = NormalizarComentarios(comentarios);
        if (comentariosNormalizados.IsFailure)
        {
            return comentariosNormalizados;
        }

        if (Estado != EstadoSolicitudApertura.Pendiente)
        {
            return SolicitudAperturaErrors.NoPendiente;
        }

        if (cupoMinimo is null || cupoMaximo is null)
        {
            return SolicitudAperturaErrors.CuposIncompletos;
        }

        var cupos = ValidarCupos(CantidadEstudiantes, cupoMinimo, cupoMaximo);
        if (cupos.IsFailure)
        {
            return cupos;
        }

        Estado = EstadoSolicitudApertura.Aceptada;
        ResueltaEn = utc;
        ResueltaPorUsuarioId = usuarioId;
        ComentariosResolucion = comentariosNormalizados.Value;
        return Result.Success();
    }

    public Result Rechazar(string? comentarios, int usuarioId, DateTime utc)
    {
        var comentariosNormalizados = NormalizarComentarios(comentarios);
        if (comentariosNormalizados.IsFailure)
        {
            return comentariosNormalizados;
        }

        if (comentariosNormalizados.Value is null)
        {
            return SolicitudAperturaErrors.ComentariosVacios;
        }

        if (Estado != EstadoSolicitudApertura.Pendiente)
        {
            return SolicitudAperturaErrors.NoPendiente;
        }

        Estado = EstadoSolicitudApertura.Rechazada;
        ResueltaEn = utc;
        ResueltaPorUsuarioId = usuarioId;
        ComentariosResolucion = comentariosNormalizados.Value;
        return Result.Success();
    }

    public Result Cancelar(string? motivo, int usuarioId, DateTime utc)
    {
        var motivoNormalizado = Normalizacion.Recortar(motivo);
        if (motivoNormalizado.Length == 0)
        {
            return SolicitudAperturaErrors.MotivoVacio;
        }

        if (motivoNormalizado.Length > LongitudMaximaMotivo)
        {
            return SolicitudAperturaErrors.MotivoDemasiadoLargo;
        }

        if (Estado != EstadoSolicitudApertura.Pendiente)
        {
            return SolicitudAperturaErrors.NoPendiente;
        }

        Estado = EstadoSolicitudApertura.Cancelada;
        CanceladaEn = utc;
        CanceladaPorUsuarioId = usuarioId;
        MotivoCancelacion = motivoNormalizado;
        return Result.Success();
    }
}
