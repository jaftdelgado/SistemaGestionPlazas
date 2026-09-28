using Sgpla.Modules.Usuarios.Domain.Cuentas;

namespace Sgpla.UnitTests.Usuarios;

public sealed class PoliticaContrasenaTests
{
    [Fact]
    public void Validar_ConSieteCaracteres_FallaConLongitudInvalida()
    {
        PoliticaContrasena.Validar("Abc12!a").Error.ShouldBe(UsuarioErrors.ContrasenaLongitudInvalida);
    }

    [Fact]
    public void Validar_ConOchoCaracteres_Permite()
    {
        PoliticaContrasena.Validar("Abcd12!a").IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Validar_Con128Caracteres_Permite()
    {
        var contrasena = "Aa1!" + new string('a', 124);

        PoliticaContrasena.Validar(contrasena).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Validar_Con129Caracteres_FallaConLongitudInvalida()
    {
        var contrasena = "Aa1!" + new string('a', 125);

        PoliticaContrasena.Validar(contrasena).Error.ShouldBe(UsuarioErrors.ContrasenaLongitudInvalida);
    }

    [Theory]
    [InlineData("abcdefg1!")]
    [InlineData("ABCDEFG1!")]
    [InlineData("Abcdefgh!")]
    [InlineData("Abcdefg12")]
    public void Validar_SinAlgunTipoDeCaracter_FallaConContrasenaDebil(string contrasena)
    {
        PoliticaContrasena.Validar(contrasena).Error.ShouldBe(UsuarioErrors.ContrasenaDebil);
    }

    [Fact]
    public void Validar_LosEspaciosCuentanParaLaLongitud()
    {
        var resultado = PoliticaContrasena.Validar("Ab1! Ab1");

        resultado.IsSuccess.ShouldBeTrue();
    }
}
