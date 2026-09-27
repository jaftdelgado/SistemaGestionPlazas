using Sgpla.Modules.Catalogos.Domain.Articulos;

namespace Sgpla.UnitTests.Catalogos.Articulos;

public sealed class ArticuloTests
{
    private const string Descripcion = "Contratación por tiempo determinado.";

    [Theory]
    [InlineData("42", "42")]
    [InlineData("  42  bis ", "42 BIS")]
    [InlineData("42\tbis", "42 BIS")]
    [InlineData("42-a", "42-A")]
    public void Crear_NormalizaElNumero(string numero, string esperado)
    {
        var resultado = Articulo.Crear(numero, Descripcion);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Numero.ShouldBe(esperado);
    }

    [Fact]
    public void Crear_RecortaLaDescripcion()
    {
        Articulo.Crear("42", $"  {Descripcion}  ").Value.Descripcion.ShouldBe(Descripcion);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinNumero_FallaConNumeroVacio(string numero)
    {
        Articulo.Crear(numero, Descripcion).Error.ShouldBe(ArticuloErrors.NumeroVacio);
    }

    [Theory]
    [InlineData("42 BÍS")]
    [InlineData("42 Ñ")]
    public void Crear_ConCaracteresNoAscii_FallaConNumeroNoAscii(string numero)
    {
        Articulo.Crear(numero, Descripcion).Error.ShouldBe(ArticuloErrors.NumeroNoAscii);
    }

    [Fact]
    public void Crear_ConNumeroDemasiadoLargo_FallaConNumeroDemasiadoLargo()
    {
        var numero = new string('1', Articulo.LongitudMaximaNumero + 1);

        Articulo.Crear(numero, Descripcion).Error.ShouldBe(ArticuloErrors.NumeroDemasiadoLargo);
    }

    [Fact]
    public void Crear_ConLongitudMaximaTrasColapsarEspacios_EsValido()
    {
        var numero = new string('1', Articulo.LongitudMaximaNumero - 2) + "    A";

        Articulo.Crear(numero, Descripcion).Value.Numero.Length.ShouldBe(Articulo.LongitudMaximaNumero);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Crear_SinDescripcion_FallaConDescripcionVacia(string? descripcion)
    {
        Articulo.Crear("42", descripcion!).Error.ShouldBe(ArticuloErrors.DescripcionVacia);
    }

    [Fact]
    public void Crear_ConDescripcionDemasiadoLarga_FallaConDescripcionDemasiadoLarga()
    {
        var descripcion = new string('a', Articulo.LongitudMaximaDescripcion + 1);

        Articulo.Crear("42", descripcion).Error.ShouldBe(ArticuloErrors.DescripcionDemasiadoLarga);
    }

    [Fact]
    public void Modificar_ConDescripcionNula_ConservaLaActual()
    {
        var articulo = Articulo.Crear("42", Descripcion).Value;

        var resultado = articulo.Modificar("43", descripcion: null);

        resultado.IsSuccess.ShouldBeTrue();
        articulo.Numero.ShouldBe("43");
        articulo.Descripcion.ShouldBe(Descripcion);
    }

    [Fact]
    public void Modificar_ConDescripcionVacia_FallaSinCambiarNada()
    {
        var articulo = Articulo.Crear("42", Descripcion).Value;

        var resultado = articulo.Modificar("43", "  ");

        resultado.Error.ShouldBe(ArticuloErrors.DescripcionVacia);
        articulo.Numero.ShouldBe("42");
        articulo.Descripcion.ShouldBe(Descripcion);
    }

    [Fact]
    public void Modificar_ConNumeroInvalido_FallaSinCambiarNada()
    {
        var articulo = Articulo.Crear("42", Descripcion).Value;

        var resultado = articulo.Modificar("é", "Otra descripción.");

        resultado.Error.ShouldBe(ArticuloErrors.NumeroNoAscii);
        articulo.Numero.ShouldBe("42");
        articulo.Descripcion.ShouldBe(Descripcion);
    }
}
