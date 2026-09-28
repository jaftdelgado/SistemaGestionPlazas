using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.SharedKernel;

namespace Sgpla.UnitTests.Usuarios;

public sealed class UsuarioTests
{
    [Fact]
    public void CrearSuperusuario_ConEspaciosYMayusculas_NormalizaCorreoYNombre()
    {
        var resultado = Usuario.CrearSuperusuario("  Ana.Lopez@Gmail.Com  ", "  Ana   López ", "hash");

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Correo.ShouldBe("ana.lopez@gmail.com");
        resultado.Value.Nombre.ShouldBe("Ana López");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CrearSuperusuario_SinCorreo_FallaConCorreoVacio(string correo)
    {
        Usuario.CrearSuperusuario(correo, "Nombre", "hash").Error.ShouldBe(UsuarioErrors.CorreoVacio);
    }

    [Fact]
    public void CrearSuperusuario_ConCorreoDemasiadoLargo_Falla()
    {
        var correo = $"{new string('a', Usuario.LongitudMaximaCorreo)}@a.mx";

        Usuario.CrearSuperusuario(correo, "Nombre", "hash").Error.ShouldBe(UsuarioErrors.CorreoDemasiadoLargo);
    }

    [Theory]
    [InlineData("sin-arroba.mx")]
    [InlineData("dos@arrobas@a.mx")]
    [InlineData("@sinusuario.mx")]
    [InlineData("usuario@sindominio")]
    [InlineData("usuario@.mx")]
    [InlineData("usuario@dominio.")]
    [InlineData("con espacio@a.mx")]
    [InlineData("conñ@a.mx")]
    public void CrearSuperusuario_ConCorreoMalFormado_FallaConFormatoInvalido(string correo)
    {
        Usuario.CrearSuperusuario(correo, "Nombre", "hash").Error.ShouldBe(UsuarioErrors.CorreoFormatoInvalido);
    }

    [Fact]
    public void CrearSuperusuario_ConCualquierDominioValido_Permite()
    {
        Usuario.CrearSuperusuario("persona@gmail.com", "Nombre", "hash").IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CrearSuperusuario_SinNombre_FallaConNombreVacio(string nombre)
    {
        Usuario.CrearSuperusuario("persona@gmail.com", nombre, "hash").Error.ShouldBe(UsuarioErrors.NombreVacio);
    }

    [Fact]
    public void CrearSuperusuario_ConNombreDemasiadoLargo_Falla()
    {
        var nombre = new string('a', Usuario.LongitudMaximaNombre + 1);

        Usuario.CrearSuperusuario("persona@gmail.com", nombre, "hash").Error.ShouldBe(UsuarioErrors.NombreDemasiadoLargo);
    }

    [Fact]
    public void CrearDgaa_ConCorreoNoInstitucional_FallaConCorreoNoInstitucional()
    {
        Usuario.CrearDgaa("persona@gmail.com", "Nombre", 1).Error.ShouldBe(UsuarioErrors.CorreoNoInstitucional);
    }

    [Fact]
    public void CrearDgaa_ConCorreoUv_Permite()
    {
        var resultado = Usuario.CrearDgaa("persona@uv.mx", "Nombre", 7);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.AreaAcademicaId.ShouldBe(7);
    }

    [Fact]
    public void CrearEntidadAcademica_ConCorreoNoInstitucional_FallaConCorreoNoInstitucional()
    {
        Usuario.CrearEntidadAcademica("persona@gmail.com", "Nombre", 1).Error.ShouldBe(UsuarioErrors.CorreoNoInstitucional);
    }

    [Fact]
    public void CrearEntidadAcademica_ConCorreoUv_Permite()
    {
        var resultado = Usuario.CrearEntidadAcademica("persona@uv.mx", "Nombre", 9);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.EntidadAcademicaId.ShouldBe(9);
    }

    [Fact]
    public void CrearSuperusuario_SoloTieneCredencial()
    {
        var usuario = Usuario.CrearSuperusuario("persona@gmail.com", "Nombre", "hash").Value;

        usuario.Rol.ShouldBe(Rol.Superusuario);
        usuario.Credencial.ShouldNotBeNull();
        usuario.PerfilDgaa.ShouldBeNull();
        usuario.PerfilEntidadAcademica.ShouldBeNull();
        usuario.AreaAcademicaId.ShouldBeNull();
        usuario.EntidadAcademicaId.ShouldBeNull();
    }

    [Fact]
    public void CrearDgaa_SoloTienePerfilDgaa()
    {
        var usuario = Usuario.CrearDgaa("persona@uv.mx", "Nombre", 3).Value;

        usuario.Rol.ShouldBe(Rol.Dgaa);
        usuario.PerfilDgaa.ShouldNotBeNull();
        usuario.PerfilEntidadAcademica.ShouldBeNull();
        usuario.Credencial.ShouldBeNull();
    }

    [Fact]
    public void CrearEntidadAcademica_SoloTienePerfilEntidadAcademica()
    {
        var usuario = Usuario.CrearEntidadAcademica("persona@uv.mx", "Nombre", 4).Value;

        usuario.Rol.ShouldBe(Rol.EntidadAcademica);
        usuario.PerfilEntidadAcademica.ShouldNotBeNull();
        usuario.PerfilDgaa.ShouldBeNull();
        usuario.Credencial.ShouldBeNull();
    }

    [Fact]
    public void CambiarNombre_ConNombreValido_Cambia()
    {
        var usuario = Usuario.CrearSuperusuario("persona@gmail.com", "Nombre", "hash").Value;

        usuario.CambiarNombre("Nombre Nuevo").IsSuccess.ShouldBeTrue();

        usuario.Nombre.ShouldBe("Nombre Nuevo");
    }

    [Fact]
    public void CambiarNombre_ConNombreVacio_NoCambiaNada()
    {
        var usuario = Usuario.CrearSuperusuario("persona@gmail.com", "Nombre Original", "hash").Value;

        var resultado = usuario.CambiarNombre("");

        resultado.Error.ShouldBe(UsuarioErrors.NombreVacio);
        usuario.Nombre.ShouldBe("Nombre Original");
    }

    [Fact]
    public void DarDeBaja_AsignaElMismoInstanteALaCuentaYALaCredencial()
    {
        var usuario = Usuario.CrearSuperusuario("persona@gmail.com", "Nombre", "hash").Value;
        var instante = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        usuario.DarDeBaja(instante);

        usuario.FechaEliminacion.ShouldBe(instante);
        usuario.Credencial!.FechaEliminacion.ShouldBe(instante);
    }

    [Fact]
    public void DarDeBaja_Idempotente_NoReemplazaLaFecha()
    {
        var usuario = Usuario.CrearSuperusuario("persona@gmail.com", "Nombre", "hash").Value;
        var primero = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var segundo = new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc);

        usuario.DarDeBaja(primero);
        usuario.DarDeBaja(segundo);

        usuario.FechaEliminacion.ShouldBe(primero);
        usuario.Credencial!.FechaEliminacion.ShouldBe(primero);
    }

    [Fact]
    public void DarDeBaja_SinCredencial_NoFalla()
    {
        var usuario = Usuario.CrearDgaa("persona@uv.mx", "Nombre", 1).Value;

        usuario.DarDeBaja(DateTime.UtcNow);

        usuario.FechaEliminacion.ShouldNotBeNull();
    }

    [Fact]
    public void EstablecerContrasena_AsignaLaFechaDeActualizacion()
    {
        var usuario = Usuario.CrearSuperusuario("persona@gmail.com", "Nombre", "hash-temporal").Value;
        var instante = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        usuario.EstablecerContrasena("hash-nuevo", instante);

        usuario.Credencial!.Contrasena.ShouldBe("hash-nuevo");
        usuario.Credencial.FechaActualizacion.ShouldBe(instante);
        usuario.CambioContrasenaPendiente.ShouldBeFalse();
    }

    [Fact]
    public void RestablecerContrasena_RegresaAFechaDeActualizacionNula()
    {
        var usuario = Usuario.CrearSuperusuario("persona@gmail.com", "Nombre", "hash-temporal").Value;
        usuario.EstablecerContrasena("hash-cambiado", DateTime.UtcNow);

        usuario.RestablecerContrasena("hash-temporal-nuevo");

        usuario.Credencial!.Contrasena.ShouldBe("hash-temporal-nuevo");
        usuario.Credencial.FechaActualizacion.ShouldBeNull();
        usuario.CambioContrasenaPendiente.ShouldBeTrue();
    }

    [Fact]
    public void ActualizarVerificador_ConservaLaFechaDeActualizacion()
    {
        var usuario = Usuario.CrearSuperusuario("persona@gmail.com", "Nombre", "hash-temporal").Value;
        var instante = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        usuario.EstablecerContrasena("hash-cambiado", instante);

        usuario.ActualizarVerificador("hash-rehash");

        usuario.Credencial!.Contrasena.ShouldBe("hash-rehash");
        usuario.Credencial.FechaActualizacion.ShouldBe(instante);
    }

    [Fact]
    public void MetodosDeContrasena_EnUnaCuentaQueNoEsSuperusuario_Lanzan()
    {
        var usuario = Usuario.CrearDgaa("persona@uv.mx", "Nombre", 1).Value;

        Should.Throw<InvalidOperationException>(() => usuario.EstablecerContrasena("x", DateTime.UtcNow));
        Should.Throw<InvalidOperationException>(() => usuario.RestablecerContrasena("x"));
        Should.Throw<InvalidOperationException>(() => usuario.ActualizarVerificador("x"));
    }
}
