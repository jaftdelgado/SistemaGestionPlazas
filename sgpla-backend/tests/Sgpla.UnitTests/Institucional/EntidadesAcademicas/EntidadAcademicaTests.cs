using Sgpla.Modules.Institucional.Domain.EntidadesAcademicas;
using Sgpla.SharedKernel;

namespace Sgpla.UnitTests.Institucional.EntidadesAcademicas;

public sealed class EntidadAcademicaTests
{
    private const string Nombre = "Facultad de Letras Españolas";
    private const string Calle = "Francisco Moreno";
    private const string Colonia = "Ferrer Guardia";
    private const string CodigoPostal = "91020";
    private const string Telefono = "2288421700";

    [Fact]
    public void Crear_ConEspaciosYMinusculas_NormalizaLaClave()
    {
        var resultado = Crear(clave: " flx1 ");

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Clave.ShouldBe("FLX1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinClave_FallaConClaveVacia(string clave)
    {
        Crear(clave: clave).Error.ShouldBe(EntidadAcademicaErrors.ClaveVacia);
    }

    [Fact]
    public void Crear_ConClaveDemasiadoLarga_FallaConClaveDemasiadoLarga()
    {
        Crear(clave: new string('A', EntidadAcademica.LongitudMaximaClave + 1)).Error
            .ShouldBe(EntidadAcademicaErrors.ClaveDemasiadoLarga);
    }

    [Theory]
    [InlineData("FLXÑ")]
    [InlineData("FLX-1")]
    public void Crear_ConClaveConCaracteresInvalidos_FallaConClaveFormatoInvalido(string clave)
    {
        Crear(clave: clave).Error.ShouldBe(EntidadAcademicaErrors.ClaveFormatoInvalido);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinNombre_FallaConNombreVacio(string nombre)
    {
        Crear(nombre: nombre).Error.ShouldBe(EntidadAcademicaErrors.NombreVacio);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinCalle_FallaConCalleVacia(string calle)
    {
        Crear(calle: calle).Error.ShouldBe(EntidadAcademicaErrors.CalleVacia);
    }

    [Fact]
    public void Crear_ConNumeroExteriorNulo_LoAcepta()
    {
        var resultado = Crear(numeroExterior: null);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.NumeroExterior.ShouldBeNull();
    }

    [Fact]
    public void Crear_ConNumeroExteriorSoloEspacios_FallaConNumeroExteriorVacio()
    {
        Crear(numeroExterior: "   ").Error.ShouldBe(EntidadAcademicaErrors.NumeroExteriorVacio);
    }

    [Fact]
    public void Crear_ConNumeroExteriorDemasiadoLargo_FallaConNumeroExteriorDemasiadoLargo()
    {
        Crear(numeroExterior: new string('1', EntidadAcademica.LongitudMaximaNumeroExterior + 1)).Error
            .ShouldBe(EntidadAcademicaErrors.NumeroExteriorDemasiadoLargo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinColonia_FallaConColoniaVacia(string colonia)
    {
        Crear(colonia: colonia).Error.ShouldBe(EntidadAcademicaErrors.ColoniaVacia);
    }

    [Theory]
    [InlineData("9102")]
    [InlineData("910200")]
    [InlineData("9102A")]
    public void Crear_ConCodigoPostalInvalido_FallaConCodigoPostalFormatoInvalido(string codigoPostal)
    {
        Crear(codigoPostal: codigoPostal).Error.ShouldBe(EntidadAcademicaErrors.CodigoPostalFormatoInvalido);
    }

    [Fact]
    public void Crear_ConCodigoPostalConCerosIniciales_ConservaLosCeros()
    {
        var resultado = Crear(codigoPostal: "00120");

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.CodigoPostal.ShouldBe("00120");
    }

    [Theory]
    [InlineData("228842170")]
    [InlineData("228 842 1700")]
    public void Crear_ConTelefonoInvalido_FallaConTelefonoFormatoInvalido(string telefono)
    {
        Crear(telefono: telefono).Error.ShouldBe(EntidadAcademicaErrors.TelefonoFormatoInvalido);
    }

    [Fact]
    public void Crear_ConTelefonoConCerosIniciales_ConservaLosCeros()
    {
        var resultado = Crear(telefono: "0228842170");

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Telefono.ShouldBe("0228842170");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_ConExtensionVacia_QuedaEnNull(string? extension)
    {
        var resultado = Crear(extension: extension);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Extension.ShouldBeNull();
    }

    [Fact]
    public void Crear_ConExtensionConCerosIniciales_ConservaLosCeros()
    {
        var resultado = Crear(extension: "007");

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Extension.ShouldBe("007");
    }

    [Fact]
    public void Modificar_ConDatosInvalidos_NoCambiaNada()
    {
        var entidad = Crear().Value;

        var resultado = entidad.Modificar("", Calle, null, Colonia, CodigoPostal, Telefono, null, 3, 87);

        resultado.IsFailure.ShouldBeTrue();
        entidad.Nombre.ShouldBe(Nombre);
        entidad.AreaAcademicaId.ShouldBe(1);
        entidad.MunicipioId.ShouldBe(1);
    }

    [Fact]
    public void Modificar_ConDatosValidos_CambiaLosCamposYPermiteElAreaYMunicipio()
    {
        var entidad = Crear().Value;

        var resultado = entidad.Modificar(
            "Facultad de Música", "Otra calle", "10", "Otra colonia", "91021", "2288421701", "222", 5, 99);

        resultado.IsSuccess.ShouldBeTrue();
        entidad.Nombre.ShouldBe("Facultad de Música");
        entidad.AreaAcademicaId.ShouldBe(5);
        entidad.MunicipioId.ShouldBe(99);
    }

    private static Result<EntidadAcademica> Crear(
        string clave = "FLX1",
        string nombre = Nombre,
        string calle = Calle,
        string? numeroExterior = null,
        string colonia = Colonia,
        string codigoPostal = CodigoPostal,
        string telefono = Telefono,
        string? extension = null) =>
        EntidadAcademica.Crear(clave, nombre, calle, numeroExterior, colonia, codigoPostal, telefono, extension, 1, 1, 1);
}
