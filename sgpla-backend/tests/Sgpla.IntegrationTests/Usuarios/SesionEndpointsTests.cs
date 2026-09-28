using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Sgpla.IntegrationTests.Infraestructura;
using Sgpla.SharedKernel;

namespace Sgpla.IntegrationTests.Usuarios;

public sealed class SesionEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private const string Ruta = "/api/v1/usuarios";

    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task IniciarSesion_SuperusuarioConContrasenaCambiada_Responde200SinCambioPendiente()
    {
        using var cliente = _api.CreateClient();
        var correo = await CrearSuperusuarioConContrasenaCambiadaAsync();

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri("/iniciar-sesion"), new { correo, contrasena = SgplaApiFactory.ContrasenaConocidaSuperusuario }, Cancelacion);
        var sesion = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        sesion.GetProperty("usuario").GetProperty("cambioContrasenaPendiente").GetBoolean().ShouldBeFalse();
        sesion.GetProperty("usuario").GetProperty("rol").GetProperty("nombre").GetString().ShouldBe("Superusuario");
    }

    [Fact]
    public async Task IniciarSesion_SuperusuarioConTemporal_Responde200ConCambioPendiente()
    {
        using var cliente = _api.CreateClient();
        var correo = await _api.CrearSuperusuarioAsync();

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri("/iniciar-sesion"), new { correo, contrasena = SgplaApiFactory.ContrasenaConocidaSuperusuario }, Cancelacion);
        var sesion = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        sesion.GetProperty("usuario").GetProperty("cambioContrasenaPendiente").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task IniciarSesion_DgaaPorLdap_Responde200ConSuArea()
    {
        using var cliente = _api.CreateClient();
        var (areaId, claveArea) = await CrearAreaAsync(cliente);
        var correo = await CrearCuentaDgaaEnBaseAsync(areaId);

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri("/iniciar-sesion"), new { correo, contrasena = LdapFalso.ContrasenaValida }, Cancelacion);
        var sesion = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        sesion.GetProperty("usuario").GetProperty("areaAcademica").GetProperty("clave").GetInt32().ShouldBe(claveArea);
        sesion.GetProperty("usuario").GetProperty("entidadAcademica").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task IniciarSesion_EntidadAcademicaPorLdap_Responde200ConSuEntidad()
    {
        using var cliente = _api.CreateClient();
        var (areaId, _) = await CrearAreaAsync(cliente);
        var (entidadId, claveEntidad) = await CrearEntidadAsync(cliente, areaId);
        var correo = await CrearCuentaEntidadAcademicaEnBaseAsync(entidadId);

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri("/iniciar-sesion"), new { correo, contrasena = LdapFalso.ContrasenaValida }, Cancelacion);
        var sesion = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        sesion.GetProperty("usuario").GetProperty("entidadAcademica").GetProperty("clave").GetString().ShouldBe(claveEntidad);
    }

    [Fact]
    public async Task IniciarSesion_SinDominioEnElCorreo_SeCompletaConUvMx()
    {
        using var cliente = _api.CreateClient();
        var (areaId, _) = await CrearAreaAsync(cliente);
        var correoCompleto = await CrearCuentaDgaaEnBaseAsync(areaId);
        var usuarioSinDominio = correoCompleto[..correoCompleto.IndexOf('@', StringComparison.Ordinal)];

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri("/iniciar-sesion"), new { correo = usuarioSinDominio, contrasena = LdapFalso.ContrasenaValida }, Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task IniciarSesion_CuentaInexistente_Responde401ConCuentaNoRegistrada()
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri("/iniciar-sesion"), new { correo = DatosUnicos.Correo("gmail.com"), contrasena = "cualquiera" }, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        problema.GetProperty("codigo").GetString().ShouldBe("Usuario.CuentaNoRegistrada");
    }

    [Fact]
    public async Task IniciarSesion_CuentaDadaDeBaja_Responde401ConCuentaNoRegistrada()
    {
        using var cliente = _api.CreateClient();
        var correo = await CrearSuperusuarioConContrasenaCambiadaAsync();
        await DarDeBajaUsuarioAsync(correo);

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri("/iniciar-sesion"),
            new { correo, contrasena = SgplaApiFactory.ContrasenaConocidaSuperusuario },
            Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        problema.GetProperty("codigo").GetString().ShouldBe("Usuario.CuentaNoRegistrada");
    }

    [Fact]
    public async Task IniciarSesion_AreaDadaDeBaja_Responde401ConAmbitoInactivo()
    {
        using var cliente = _api.CreateClient();
        var (areaId, _) = await CrearAreaAsync(cliente);
        var correo = await CrearCuentaDgaaEnBaseAsync(areaId);
        await DarDeBajaAreaAsync(areaId);

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri("/iniciar-sesion"), new { correo, contrasena = LdapFalso.ContrasenaValida }, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        problema.GetProperty("codigo").GetString().ShouldBe("Usuario.AmbitoInactivo");
    }

    [Fact]
    public async Task IniciarSesion_SuperusuarioConContrasenaIncorrecta_Responde401()
    {
        using var cliente = _api.CreateClient();
        var correo = await CrearSuperusuarioConContrasenaCambiadaAsync();

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri("/iniciar-sesion"), new { correo, contrasena = "Incorrecta123!" }, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        problema.GetProperty("codigo").GetString().ShouldBe("Usuario.CredencialesInvalidas");
    }

    [Fact]
    public async Task IniciarSesion_LdapConCredencialesInvalidas_Responde401()
    {
        using var cliente = _api.CreateClient();
        var (areaId, _) = await CrearAreaAsync(cliente);
        var correo = await CrearCuentaDgaaEnBaseAsync(areaId);

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri("/iniciar-sesion"), new { correo, contrasena = "mala" }, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        problema.GetProperty("codigo").GetString().ShouldBe("Usuario.CredencialesInvalidas");
    }

    [Fact]
    public async Task IniciarSesion_ConLdapCaido_Responde503()
    {
        using var cliente = _api.CreateClient();
        var (areaId, _) = await CrearAreaAsync(cliente);
        var correo = $"ldap-caido-{DatosUnicos.Correo("uv.mx")}";
        await RegistrarDgaaConCorreoAsync(correo, areaId);

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri("/iniciar-sesion"), new { correo, contrasena = LdapFalso.ContrasenaValida }, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        problema.GetProperty("codigo").GetString().ShouldBe("Usuario.LdapNoDisponible");
    }

    [Theory]
    [InlineData("", "contrasena")]
    [InlineData("correo@gmail.com", "")]
    public async Task IniciarSesion_ConCorreoOContrasenaVacios_Responde400(string correo, string contrasena)
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.PostAsJsonAsync(Uri("/iniciar-sesion"), new { correo, contrasena }, Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ObtenerSesion_ConToken_Responde200()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri("/sesion"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ObtenerSesion_SinToken_Responde401()
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.GetAsync(Uri("/sesion"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        problema.GetProperty("codigo").GetString().ShouldBe("Autenticacion.NoAutenticado");
    }

    [Fact]
    public async Task ObtenerSesion_ConTokenAlterado_Responde401()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", cliente.DefaultRequestHeaders.Authorization!.Parameter + "x");

        using var respuesta = await cliente.GetAsync(Uri("/sesion"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ObtenerSesion_ConTokenVencido_Responde401()
    {
        using var cliente = _api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", SgplaApiFactory.EmitirTokenVencido(1, Rol.Superusuario));

        using var respuesta = await cliente.GetAsync(Uri("/sesion"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        problema.GetProperty("codigo").GetString().ShouldBe("Autenticacion.NoAutenticado");
    }

    [Fact]
    public async Task ObtenerSesion_ConCuentaDadaDeBajaDespuesDeEmitirElToken_Responde401()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        using var sesionPrevia = await cliente.GetAsync(Uri("/sesion"), Cancelacion);
        var correo = (await Leer(sesionPrevia)).GetProperty("correo").GetString()!;
        await DarDeBajaUsuarioAsync(correo);

        using var respuesta = await cliente.GetAsync(Uri("/sesion"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListarCuentas_ConContrasenaPendiente_Responde403ConCambioContrasenaPendiente()
    {
        var correo = await _api.CrearSuperusuarioAsync();
        using var cliente = await ClienteConSesionAsync(correo, SgplaApiFactory.ContrasenaConocidaSuperusuario);

        using var respuesta = await cliente.GetAsync(new Uri($"{Ruta}/cuentas", UriKind.Relative), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        problema.GetProperty("codigo").GetString().ShouldBe("Autenticacion.CambioContrasenaPendiente");
    }

    [Fact]
    public async Task CambiarContrasena_ConDatosValidos_Responde200ConTokenQueYaNoEstaLimitado()
    {
        var correo = await _api.CrearSuperusuarioAsync();
        using var cliente = await ClienteConSesionAsync(correo, SgplaApiFactory.ContrasenaConocidaSuperusuario);

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri("/sesion/cambiar-contrasena"),
            new { contrasenaActual = SgplaApiFactory.ContrasenaConocidaSuperusuario, contrasenaNueva = "NuevaContrasena1!" },
            Cancelacion);
        var sesion = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        sesion.GetProperty("usuario").GetProperty("cambioContrasenaPendiente").GetBoolean().ShouldBeFalse();

        using var clienteNuevo = _api.CreateClient();
        clienteNuevo.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", sesion.GetProperty("token").GetString());
        using var respuestaCuentas = await clienteNuevo.GetAsync(new Uri($"{Ruta}/cuentas", UriKind.Relative), Cancelacion);
        respuestaCuentas.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CambiarContrasena_ConContrasenaActualIncorrecta_Responde400()
    {
        var correo = await _api.CrearSuperusuarioAsync();
        using var cliente = await ClienteConSesionAsync(correo, SgplaApiFactory.ContrasenaConocidaSuperusuario);

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri("/sesion/cambiar-contrasena"),
            new { contrasenaActual = "Incorrecta1!", contrasenaNueva = "NuevaContrasena1!" },
            Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("codigo").GetString().ShouldBe("Usuario.ContrasenaActualIncorrecta");
    }

    [Fact]
    public async Task CambiarContrasena_ConContrasenaNuevaDebil_Responde400()
    {
        var correo = await _api.CrearSuperusuarioAsync();
        using var cliente = await ClienteConSesionAsync(correo, SgplaApiFactory.ContrasenaConocidaSuperusuario);

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri("/sesion/cambiar-contrasena"),
            new { contrasenaActual = SgplaApiFactory.ContrasenaConocidaSuperusuario, contrasenaNueva = "debil" },
            Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("codigo").GetString().ShouldBe("Usuario.ContrasenaLongitudInvalida");
    }

    [Fact]
    public async Task CambiarContrasena_IgualALaActual_Responde400()
    {
        var correo = await _api.CrearSuperusuarioAsync();
        using var cliente = await ClienteConSesionAsync(correo, SgplaApiFactory.ContrasenaConocidaSuperusuario);

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri("/sesion/cambiar-contrasena"),
            new
            {
                contrasenaActual = SgplaApiFactory.ContrasenaConocidaSuperusuario,
                contrasenaNueva = SgplaApiFactory.ContrasenaConocidaSuperusuario,
            },
            Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("codigo").GetString().ShouldBe("Usuario.ContrasenaNuevaIgualActual");
    }

    [Fact]
    public async Task CambiarContrasena_ConCuentaUv_Responde409()
    {
        using var clienteAnonimo = _api.CreateClient();
        var (areaId, _) = await CrearAreaAsync(clienteAnonimo);
        using var cliente = await _api.CrearClienteDgaaAsync(areaId);

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri("/sesion/cambiar-contrasena"),
            new { contrasenaActual = "cualquiera", contrasenaNueva = "NuevaContrasena1!" },
            Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("Usuario.CambioContrasenaNoAplica");
    }

    [Fact]
    public async Task CerrarSesion_Responde204()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.PostAsync(Uri("/cerrar-sesion"), content: null, Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task NingunLogDeLaPrueba_ContieneLaContrasenaNiElCorreo()
    {
        using var cliente = _api.CreateClient();
        var correo = await CrearSuperusuarioConContrasenaCambiadaAsync();

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri("/iniciar-sesion"), new { correo, contrasena = SgplaApiFactory.ContrasenaConocidaSuperusuario }, Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);

        _api.Logs.Entradas.ShouldNotContain(entrada => entrada.Contiene(correo));
        _api.Logs.Entradas.ShouldNotContain(entrada => entrada.Contiene(SgplaApiFactory.ContrasenaConocidaSuperusuario));
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static Uri Uri(string sufijo) => new($"{Ruta}{sufijo}", UriKind.Relative);

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>(Cancelacion);

    private async Task<HttpClient> ClienteConSesionAsync(string correo, string contrasena)
    {
        var cliente = _api.CreateClient();
        using var respuesta = await cliente.PostAsJsonAsync(Uri("/iniciar-sesion"), new { correo, contrasena }, Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var sesion = await Leer(respuesta);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.GetProperty("token").GetString());
        return cliente;
    }

    private async Task<string> CrearSuperusuarioConContrasenaCambiadaAsync()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        using var respuesta = await cliente.GetAsync(Uri("/sesion"), Cancelacion);
        return (await Leer(respuesta)).GetProperty("correo").GetString()!;
    }

    private static async Task<(int Id, int Clave)> CrearAreaAsync(HttpClient cliente)
    {
        var clave = DatosUnicos.ClaveEntera();
        using var respuesta = await cliente.PostAsJsonAsync(
            new Uri("/api/v1/institucional/areas-academicas", UriKind.Relative),
            new { clave, nombre = "Facultad de Prueba", telefono = "2288421700", extension = (string?)null },
            Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        var creada = await Leer(respuesta);
        return (creada.GetProperty("id").GetInt32(), clave);
    }

    private static async Task<(int Id, string Clave)> CrearEntidadAsync(HttpClient cliente, int areaAcademicaId)
    {
        var clave = DatosUnicos.ClaveAlfanumerica();
        using var respuesta = await cliente.PostAsJsonAsync(
            new Uri("/api/v1/institucional/entidades-academicas", UriKind.Relative),
            new
            {
                clave,
                nombre = "Entidad de Prueba",
                calle = "Calle de prueba",
                numeroExterior = (string?)null,
                colonia = "Colonia de prueba",
                codigoPostal = "91020",
                telefono = "2288421700",
                extension = (string?)null,
                campusId = 1,
                areaAcademicaId,
                municipioId = 87,
            },
            Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        var creada = await Leer(respuesta);
        return (creada.GetProperty("id").GetInt32(), clave);
    }

    private async Task<string> CrearCuentaDgaaEnBaseAsync(int areaAcademicaId)
    {
        var correo = DatosUnicos.Correo("uv.mx");
        await RegistrarDgaaConCorreoAsync(correo, areaAcademicaId);
        return correo;
    }

    private async Task RegistrarDgaaConCorreoAsync(string correo, int areaAcademicaId)
    {
        await using var conexion = new SqlConnection(sqlServer.CadenaConexion);
        await conexion.OpenAsync(Cancelacion);
        await using var comando = new SqlCommand(
            """
            INSERT INTO usuarios.usuario (correo, nombre, rol_id) OUTPUT INSERTED.id VALUES (@correo, N'DGAA de prueba', 2);
            """,
            conexion);
        comando.Parameters.AddWithValue("@correo", correo);
        var usuarioId = (int)(await comando.ExecuteScalarAsync(Cancelacion))!;

        await using var comandoPerfil = new SqlCommand(
            "INSERT INTO usuarios.usuario_dgaa (usuario_id, area_academica_id) VALUES (@usuarioId, @areaId);", conexion);
        comandoPerfil.Parameters.AddWithValue("@usuarioId", usuarioId);
        comandoPerfil.Parameters.AddWithValue("@areaId", areaAcademicaId);
        await comandoPerfil.ExecuteNonQueryAsync(Cancelacion);
    }

    private async Task<string> CrearCuentaEntidadAcademicaEnBaseAsync(int entidadAcademicaId)
    {
        var correo = DatosUnicos.Correo("uv.mx");

        await using var conexion = new SqlConnection(sqlServer.CadenaConexion);
        await conexion.OpenAsync(Cancelacion);
        await using var comando = new SqlCommand(
            """
            INSERT INTO usuarios.usuario (correo, nombre, rol_id) OUTPUT INSERTED.id VALUES (@correo, N'Entidad de prueba', 3);
            """,
            conexion);
        comando.Parameters.AddWithValue("@correo", correo);
        var usuarioId = (int)(await comando.ExecuteScalarAsync(Cancelacion))!;

        await using var comandoPerfil = new SqlCommand(
            "INSERT INTO usuarios.usuario_entidad_academica (usuario_id, entidad_academica_id) VALUES (@usuarioId, @entidadId);",
            conexion);
        comandoPerfil.Parameters.AddWithValue("@usuarioId", usuarioId);
        comandoPerfil.Parameters.AddWithValue("@entidadId", entidadAcademicaId);
        await comandoPerfil.ExecuteNonQueryAsync(Cancelacion);

        return correo;
    }

    private async Task DarDeBajaUsuarioAsync(string correo)
    {
        await using var conexion = new SqlConnection(sqlServer.CadenaConexion);
        await conexion.OpenAsync(Cancelacion);
        await using var comando = new SqlCommand(
            "UPDATE usuarios.usuario SET fecha_eliminacion = SYSUTCDATETIME() WHERE correo = @correo;", conexion);
        comando.Parameters.AddWithValue("@correo", correo);
        await comando.ExecuteNonQueryAsync(Cancelacion);
    }

    private async Task DarDeBajaAreaAsync(int areaId)
    {
        await using var conexion = new SqlConnection(sqlServer.CadenaConexion);
        await conexion.OpenAsync(Cancelacion);
        await using var comando = new SqlCommand(
            "UPDATE academico.area_academica SET fecha_eliminacion = SYSUTCDATETIME() WHERE id = @id;", conexion);
        comando.Parameters.AddWithValue("@id", areaId);
        await comando.ExecuteNonQueryAsync(Cancelacion);
    }
}
