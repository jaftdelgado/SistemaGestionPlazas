using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;

namespace Sgpla.UnitTests.SolicitudesApertura;

public sealed class SolicitudAperturaTests
{
    [Fact]
    public void Datos_CreaNormalizaSeccionYConservaSaltosDeLineaInternos()
    {
        var justificacion = "  Primera línea.\n\nSegunda línea.  ";

        var datos = DatosSolicitudApertura.Crear(" a2 ", 25, justificacion).Value;

        datos.Seccion.ShouldBe("A2");
        datos.Justificacion.ShouldBe("Primera línea.\n\nSegunda línea.");
    }

    [Theory]
    [InlineData(null, 25, "Texto", "SolicitudApertura.SeccionVacia", "Seccion")]
    [InlineData("   ", 25, "Texto", "SolicitudApertura.SeccionVacia", "Seccion")]
    [InlineData("A123456789012345678901", 25, "Texto", "SolicitudApertura.SeccionDemasiadoLarga", "Seccion")]
    [InlineData("A 2", 25, "Texto", "SolicitudApertura.SeccionFormatoInvalido", "Seccion")]
    [InlineData("Á2", 25, "Texto", "SolicitudApertura.SeccionFormatoInvalido", "Seccion")]
    [InlineData("A2", 0, "Texto", "SolicitudApertura.CantidadNoPositiva", "CantidadEstudiantes")]
    [InlineData("A2", 25, "  ", "SolicitudApertura.JustificacionVacia", "Justificacion")]
    public void Datos_ConDatoInvalidoDevuelvePrimerErrorYCampo(
        string? seccion,
        int cantidad,
        string justificacion,
        string codigo,
        string campo)
    {
        var resultado = DatosSolicitudApertura.Crear(seccion, cantidad, justificacion);

        resultado.Error.Code.ShouldBe(codigo);
        resultado.Error.Campo.ShouldBe(campo);
    }

    [Fact]
    public void Datos_ConJustificacionDeMasDeDosMilCaracteres_FallaEnJustificacion()
    {
        var resultado = DatosSolicitudApertura.Crear("A2", 25, new string('x', 2001));

        resultado.Error.ShouldBe(SolicitudAperturaErrors.JustificacionDemasiadoLarga);
        resultado.Error.Campo.ShouldBe("Justificacion");
    }

    [Fact]
    public void Crear_NacePendienteConActorYFecha()
    {
        var datos = DatosSolicitudApertura.Crear("A2", 25, "Solicitud válida.").Value;
        var archivo = ArchivoSolicitudApertura.Crear("oficio.pdf", 5, new byte[32], "solicitudes-apertura/a.pdf", 44, Fecha);

        var solicitud = SolicitudApertura.Crear(datos, 301, 99, archivo, 44, Fecha);

        solicitud.Estado.ShouldBe(EstadoSolicitudApertura.Pendiente);
        solicitud.CreadaPorUsuarioId.ShouldBe(44);
        solicitud.CreadaEn.ShouldBe(Fecha);
    }

    [Theory]
    [InlineData(12, null, null, true)]
    [InlineData(10, 10, null, true)]
    [InlineData(40, null, 40, true)]
    [InlineData(10, 10, 40, true)]
    [InlineData(40, 10, 40, true)]
    [InlineData(9, 10, 40, false)]
    [InlineData(41, 10, 40, false)]
    public void ValidarCupos_AdmiteSoloLosValoresDentroDeLosLimites(
        int cantidad,
        int? minimo,
        int? maximo,
        bool valido)
    {
        var resultado = SolicitudApertura.ValidarCupos(cantidad, minimo, maximo);

        resultado.IsSuccess.ShouldBe(valido);
        if (!valido)
        {
            resultado.Error.ShouldBe(SolicitudAperturaErrors.CantidadFueraDeCupos);
        }
    }

    private static readonly DateTime Fecha = new(2026, 10, 1, 15, 4, 5, DateTimeKind.Utc);
}
