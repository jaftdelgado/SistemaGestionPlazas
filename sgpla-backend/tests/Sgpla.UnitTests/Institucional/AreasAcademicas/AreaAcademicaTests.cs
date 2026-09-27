using Sgpla.Modules.Institucional.Domain.AreasAcademicas;

namespace Sgpla.UnitTests.Institucional.AreasAcademicas;

public sealed class AreaAcademicaTests
{
    [Fact]
    public void Crear_ConClaveNoPositiva_FallaConClaveNoPositiva()
    {
        AreaAcademica.Crear(0, "Facultad de Letras", "2288421700", null).Error
            .ShouldBe(AreaAcademicaErrors.ClaveNoPositiva);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinNombre_FallaConNombreVacio(string nombre)
    {
        AreaAcademica.Crear(1, nombre, "2288421700", null).Error.ShouldBe(AreaAcademicaErrors.NombreVacio);
    }

    [Fact]
    public void Crear_ConNombreDemasiadoLargo_FallaConNombreDemasiadoLargo()
    {
        var nombre = new string('a', AreaAcademica.LongitudMaximaNombre + 1);

        AreaAcademica.Crear(1, nombre, "2288421700", null).Error.ShouldBe(AreaAcademicaErrors.NombreDemasiadoLargo);
    }

    [Fact]
    public void Crear_ConEspaciosYRepetidos_NormalizaElNombre()
    {
        var resultado = AreaAcademica.Crear(1, "  Facultad   de  Letras ", "2288421700", null);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Nombre.ShouldBe("Facultad de Letras");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinTelefono_FallaConTelefonoVacio(string telefono)
    {
        AreaAcademica.Crear(1, "Facultad de Letras", telefono, null).Error.ShouldBe(AreaAcademicaErrors.TelefonoVacio);
    }

    [Theory]
    [InlineData("228842170")]
    [InlineData("22884217000")]
    [InlineData("228 842 1700")]
    [InlineData("228842170A")]
    public void Crear_ConTelefonoInvalido_FallaConTelefonoFormatoInvalido(string telefono)
    {
        AreaAcademica.Crear(1, "Facultad de Letras", telefono, null).Error
            .ShouldBe(AreaAcademicaErrors.TelefonoFormatoInvalido);
    }

    [Fact]
    public void Crear_ConTelefonoConCerosIniciales_ConservaLosCeros()
    {
        var resultado = AreaAcademica.Crear(1, "Facultad de Letras", "0228842170", null);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Telefono.ShouldBe("0228842170");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_ConExtensionVacia_QuedaEnNull(string? extension)
    {
        var resultado = AreaAcademica.Crear(1, "Facultad de Letras", "2288421700", extension);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Extension.ShouldBeNull();
    }

    [Theory]
    [InlineData("12345678901")]
    [InlineData("1234A")]
    public void Crear_ConExtensionInvalida_FallaConExtensionFormatoInvalido(string extension)
    {
        AreaAcademica.Crear(1, "Facultad de Letras", "2288421700", extension).Error
            .ShouldBe(AreaAcademicaErrors.ExtensionFormatoInvalido);
    }

    [Fact]
    public void Crear_ConExtensionConCerosIniciales_ConservaLosCeros()
    {
        var resultado = AreaAcademica.Crear(1, "Facultad de Letras", "2288421700", "007");

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Extension.ShouldBe("007");
    }

    [Fact]
    public void Modificar_ConDatosValidos_CambiaNombreTelefonoYExtension()
    {
        var area = AreaAcademica.Crear(1, "Facultad de Letras", "2288421700", "11350").Value;

        var resultado = area.Modificar("Facultad de Física", "2288421701", "11351");

        resultado.IsSuccess.ShouldBeTrue();
        area.Nombre.ShouldBe("Facultad de Física");
        area.Telefono.ShouldBe("2288421701");
        area.Extension.ShouldBe("11351");
    }

    [Fact]
    public void Modificar_SinExtension_LaQuita()
    {
        var area = AreaAcademica.Crear(1, "Facultad de Letras", "2288421700", "11350").Value;

        area.Modificar("Facultad de Letras", "2288421700", null);

        area.Extension.ShouldBeNull();
    }

    [Fact]
    public void Modificar_ConDatosInvalidos_NoCambiaNada()
    {
        var area = AreaAcademica.Crear(1, "Facultad de Letras", "2288421700", "11350").Value;

        var resultado = area.Modificar("", "2288421700", "11350");

        resultado.IsFailure.ShouldBeTrue();
        area.Nombre.ShouldBe("Facultad de Letras");
        area.Telefono.ShouldBe("2288421700");
        area.Extension.ShouldBe("11350");
    }

    [Fact]
    public void DarDeBaja_LlamadaDosVeces_ConservaLaPrimeraFecha()
    {
        var area = AreaAcademica.Crear(1, "Facultad de Letras", "2288421700", null).Value;
        var primeraFecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        area.DarDeBaja(primeraFecha);
        area.DarDeBaja(primeraFecha.AddDays(1));

        area.FechaEliminacion.ShouldBe(primeraFecha);
    }
}
