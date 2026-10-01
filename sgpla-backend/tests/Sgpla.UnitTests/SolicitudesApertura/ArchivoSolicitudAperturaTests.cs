using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;

namespace Sgpla.UnitTests.SolicitudesApertura;

public sealed class ArchivoSolicitudAperturaTests
{
    [Fact]
    public void ValidarOficio_ExtraeNombreDeWindowsYDeLinux()
    {
        ArchivoSolicitudApertura.ValidarOficio("C:\\fakepath\\oficio.pdf", "application/pdf", 5, 100).Value.ShouldBe("oficio.pdf");
        ArchivoSolicitudApertura.ValidarOficio("carpeta/oficio.pdf", "application/pdf", 5, 100).Value.ShouldBe("oficio.pdf");
    }

    [Fact]
    public void ValidarOficio_AceptaMimePdfSinDistinguirMayusculas()
    {
        var resultado = ArchivoSolicitudApertura.ValidarOficio("oficio.pdf", "application/PDF", 5, 100);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.ShouldBe("oficio.pdf");
    }

    [Theory]
    [InlineData("", 1, "application/pdf", "ArchivoSolicitudApertura.NombreVacio")]
    [InlineData("oficio.pdf", 0, "application/pdf", "ArchivoSolicitudApertura.Vacio")]
    [InlineData("oficio.pdf", 101, "application/pdf", "ArchivoSolicitudApertura.DemasiadoGrande")]
    [InlineData("oficio.pdf", 5, "text/plain", "ArchivoSolicitudApertura.NoEsPdf")]
    public void ValidarOficio_DetectaElPrimerError(string nombre, long tamano, string tipo, string codigo)
    {
        var resultado = ArchivoSolicitudApertura.ValidarOficio(nombre, tipo, tamano, 100);

        resultado.Error.Code.ShouldBe(codigo);
        resultado.Error.Campo.ShouldBe("Oficio");
    }

    [Fact]
    public void ValidarOficio_NombreDemasiadoLargoFallaAntesDeRevisarTamano()
    {
        var resultado = ArchivoSolicitudApertura.ValidarOficio(
            new string('x', ArchivoSolicitudApertura.LongitudMaximaNombre + 1), "text/plain", 0, 100);

        resultado.Error.ShouldBe(ArchivoSolicitudAperturaErrors.NombreDemasiadoLargo);
    }

    [Fact]
    public void ValidarOficio_ElErrorDeTamanoIncluyeElMaximoEnFormatoInvariante()
    {
        var resultado = ArchivoSolicitudApertura.ValidarOficio("oficio.pdf", "application/pdf", 101, 100);

        resultado.Error.Message.ShouldBe("El oficio supera el tamaño máximo de 100 bytes.");
    }

    [Theory]
    [InlineData("%PDF-1234", true)]
    [InlineData("%PDF", false)]
    [InlineData("{PDF-1234", false)]
    public void TieneFirmaPdf_ValidaLosPrimerosCincoBytes(string contenido, bool esperado)
    {
        ArchivoSolicitudApertura.TieneFirmaPdf(System.Text.Encoding.ASCII.GetBytes(contenido)).ShouldBe(esperado);
    }
}
