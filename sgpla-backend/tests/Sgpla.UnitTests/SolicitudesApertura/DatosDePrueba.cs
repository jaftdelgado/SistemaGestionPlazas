using Sgpla.Modules.OfertaEducativa.Application.Contracts;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;

namespace Sgpla.UnitTests.SolicitudesApertura;

/// <summary>Solicitud y experiencia educativa de partida de las pruebas de los comandos del ciclo de vida.</summary>
internal static class DatosDePrueba
{
    public const int SolicitudId = 12;
    public const int ExperienciaId = 301;
    public const int EntidadId = 7;
    public const int AreaId = 3;
    public const string ClaveOficioOriginal = "solicitudes-apertura/original.pdf";

    public static readonly DateTime Fecha = new(2026, 10, 1, 15, 4, 5, DateTimeKind.Utc);

    public static ExperienciaEducativaResumen Experiencia(
        int? cupoMinimo = 10,
        int? cupoMaximo = 40,
        int entidadId = EntidadId,
        int areaId = AreaId) =>
        new(
            ExperienciaId,
            "ISOF",
            "00001",
            "Programación",
            cupoMinimo,
            cupoMaximo,
            true,
            7,
            "ISOF-14",
            5,
            "Ingeniería de Software",
            1,
            "Escolarizada",
            entidadId,
            "FEI",
            "Facultad de Estadística e Informática",
            areaId);

    /// <summary>Solicitud PENDIENTE de la experiencia <see cref="ExperienciaId"/>, con un oficio ya guardado.</summary>
    public static SolicitudApertura Solicitud(int cantidad = 25)
    {
        var datos = DatosSolicitudApertura.Crear("A2", cantidad, "Solicitud válida.").Value;
        var oficio = ArchivoSolicitudApertura.Crear("original.pdf", 8, new byte[32], ClaveOficioOriginal, 44, Fecha);
        return SolicitudApertura.Crear(datos, ExperienciaId, 92, oficio, 44, Fecha);
    }
}
