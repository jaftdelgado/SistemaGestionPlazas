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
}
