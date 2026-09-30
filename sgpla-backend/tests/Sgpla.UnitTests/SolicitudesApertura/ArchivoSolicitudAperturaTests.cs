using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.SolicitudesApertura.Application.SolicitudesApertura;
using Sgpla.Modules.SolicitudesApertura.Domain.SolicitudesApertura;

namespace Sgpla.UnitTests.SolicitudesApertura;

public sealed class OficioSolicitudAperturaTests
{
    [Fact]
    public void ValidarOficio_SinArchivo_FallaComoObligatorio()
    {
        var resultado = OficioSolicitudApertura.Validar(null, 100);

        resultado.Error.ShouldBe(ArchivoSolicitudAperturaErrors.Obligatorio);
        resultado.Error.Campo.ShouldBe("Oficio");
    }

    [Fact]
    public void ValidarOficio_ExtraeNombreDeWindowsYDeLinux()
    {
        OficioSolicitudApertura.Validar(Recibido("C:\\fakepath\\oficio.pdf"), 100).Value.ShouldBe("oficio.pdf");
        OficioSolicitudApertura.Validar(Recibido("carpeta/oficio.pdf"), 100).Value.ShouldBe("oficio.pdf");
    }

    [Fact]
    public void ValidarOficio_AceptaMimePdfSinDistinguirMayusculas()
    {
        var resultado = OficioSolicitudApertura.Validar(Recibido(tipo: "application/PDF"), 100);

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
        var resultado = OficioSolicitudApertura.Validar(Recibido(nombre, tamano, tipo), 100);

        resultado.Error.Code.ShouldBe(codigo);
        resultado.Error.Campo.ShouldBe("Oficio");
    }

    [Fact]
    public void ValidarOficio_NombreDemasiadoLargoFallaAntesDeRevisarTamano()
    {
        var resultado = OficioSolicitudApertura.Validar(
            Recibido(new string('x', ArchivoSolicitudApertura.LongitudMaximaNombre + 1), 0, "text/plain"), 100);

        resultado.Error.ShouldBe(ArchivoSolicitudAperturaErrors.NombreDemasiadoLargo);
    }

    [Fact]
    public void ValidarOficio_ElErrorDeTamanoIncluyeElMaximoEnFormatoInvariante()
    {
        var resultado = OficioSolicitudApertura.Validar(Recibido(tamano: 101), 100);

        resultado.Error.Message.ShouldBe("El oficio supera el tamaño máximo de 100 bytes.");
    }

    [Theory]
    [InlineData("%PDF-1234", true)]
    [InlineData("%PDF", false)]
    [InlineData("{PDF-1234", false)]
    public void TieneFirmaPdf_ValidaLosPrimerosCincoBytes(string contenido, bool esperado)
    {
        OficioSolicitudApertura.TieneFirmaPdf(System.Text.Encoding.ASCII.GetBytes(contenido)).ShouldBe(esperado);
    }

    private static ArchivoRecibido Recibido(
        string nombre = "oficio.pdf",
        long tamano = 5,
        string tipo = "application/pdf") => new(nombre, tipo, tamano, () => new MemoryStream("%PDF-"u8.ToArray()));
}
