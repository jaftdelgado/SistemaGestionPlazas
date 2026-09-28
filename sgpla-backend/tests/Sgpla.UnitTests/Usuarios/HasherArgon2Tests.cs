using Microsoft.Extensions.Options;
using Sgpla.Modules.Usuarios.Application.Autenticacion;
using Sgpla.Modules.Usuarios.Infrastructure.Autenticacion;

namespace Sgpla.UnitTests.Usuarios;

public sealed class HasherArgon2Tests
{
    private readonly HasherArgon2 _hasher = new(Options.Create(new Argon2Options()));

    [Fact]
    public void Hashear_EmpiezaConElEncabezadoPhcEsperado()
    {
        _hasher.Hashear("Contrasena1!").ShouldStartWith("$argon2id$v=19$m=19456,t=2,p=1$");
    }

    [Fact]
    public void Hashear_DosVecesLaMismaContrasena_ProduceHashesDistintos()
    {
        var primero = _hasher.Hashear("Contrasena1!");
        var segundo = _hasher.Hashear("Contrasena1!");

        primero.ShouldNotBe(segundo);
    }

    [Fact]
    public void Verificar_ConLaContrasenaCorrecta_RespondeCorrecta()
    {
        var hash = _hasher.Hashear("Contrasena1!");

        _hasher.Verificar(hash, "Contrasena1!").ShouldBe(VerificacionContrasena.Correcta);
    }

    [Fact]
    public void Verificar_ConLaContrasenaIncorrecta_RespondeIncorrecta()
    {
        var hash = _hasher.Hashear("Contrasena1!");

        _hasher.Verificar(hash, "OtraContrasena1!").ShouldBe(VerificacionContrasena.Incorrecta);
    }

    [Fact]
    public void Verificar_ConParametroMenorAlConfigurado_PideRehash()
    {
        var hasherDebil = new HasherArgon2(Options.Create(new Argon2Options { MemoriaKib = 19456, Iteraciones = 2, Paralelismo = 1 }));
        var hash = hasherDebil.Hashear("Contrasena1!");
        var hasherActual = new HasherArgon2(Options.Create(new Argon2Options { MemoriaKib = 47104, Iteraciones = 3, Paralelismo = 1 }));

        hasherActual.Verificar(hash, "Contrasena1!").ShouldBe(VerificacionContrasena.CorrectaRequiereRehash);
    }

    [Fact]
    public void Verificar_ConUnaCadenaIlegible_CuentaComoIncorrecta()
    {
        _hasher.Verificar("esto-no-es-un-verificador-argon2", "Contrasena1!").ShouldBe(VerificacionContrasena.Incorrecta);
    }
}
