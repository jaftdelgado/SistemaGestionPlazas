using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests.Usuarios;

public sealed class CuentaEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private const string Ruta = "/api/v1/usuarios/cuentas";
    private const string RutaIniciarSesion = "/api/v1/usuarios/iniciar-sesion";
    private const string RutaSesion = "/api/v1/usuarios/sesion";

    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Crear_Superusuario_Responde201ConTemporalYPermiteIniciarSesion()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var correo = DatosUnicos.Correo("gmail.com");

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(),
            new { correo, nombre = "  Admin   Nuevo ", rolId = 1, areaAcademicaId = (int?)null, entidadAcademicaId = (int?)null },
            Cancelacion);
        var creada = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var cuenta = creada.GetProperty("cuenta");
        cuenta.GetProperty("correo").GetString().ShouldBe(correo.ToLowerInvariant());
        cuenta.GetProperty("nombre").GetString().ShouldBe("Admin Nuevo");
        var temporal = creada.GetProperty("contrasenaTemporal").GetString();
        temporal.ShouldNotBeNullOrEmpty();
        respuesta.Headers.Location.ShouldNotBeNull()
            .AbsolutePath.ShouldBe($"{Ruta}/{cuenta.GetProperty("id").GetInt32()}");

        using var clienteAnonimo = _api.CreateClient();
        using var login = await clienteAnonimo.PostAsJsonAsync(
            new Uri(RutaIniciarSesion, UriKind.Relative), new { correo, contrasena = temporal }, Cancelacion);
        var sesion = await Leer(login);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        sesion.GetProperty("usuario").GetProperty("cambioContrasenaPendiente").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Crear_Dgaa_Responde201ConSuAreaYSinTemporal()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearAreaAsync(cliente);
        var correo = DatosUnicos.Correo("uv.mx");

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(), new { correo, nombre = "DGAA de prueba", rolId = 2, areaAcademicaId = areaId, entidadAcademicaId = (int?)null },
            Cancelacion);
        var creada = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var cuenta = creada.GetProperty("cuenta");
        cuenta.GetProperty("areaAcademica").GetProperty("id").GetInt32().ShouldBe(areaId);
        cuenta.GetProperty("entidadAcademica").ValueKind.ShouldBe(JsonValueKind.Null);
        creada.GetProperty("contrasenaTemporal").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Crear_EntidadAcademica_Responde201ConSuEntidadYSinTemporal()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearAreaAsync(cliente);
        var entidadId = await CrearEntidadAsync(cliente, areaId);
        var correo = DatosUnicos.Correo("uv.mx");

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(),
            new { correo, nombre = "Entidad de prueba", rolId = 3, areaAcademicaId = (int?)null, entidadAcademicaId = entidadId },
            Cancelacion);
        var creada = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var cuenta = creada.GetProperty("cuenta");
        cuenta.GetProperty("entidadAcademica").GetProperty("id").GetInt32().ShouldBe(entidadId);
        creada.GetProperty("contrasenaTemporal").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Crear_DgaaSinArea_Responde400ConErrorEnAreaAcademicaId()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(),
            new
            {
                correo = DatosUnicos.Correo("uv.mx"),
                nombre = "Nombre",
                rolId = 2,
                areaAcademicaId = (int?)null,
                entidadAcademicaId = (int?)null,
            },
            Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty("areaAcademicaId", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Crear_SuperusuarioConAreaDeMas_Responde400ConErrorEnAreaAcademicaId()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(),
            new
            {
                correo = DatosUnicos.Correo("gmail.com"),
                nombre = "Nombre",
                rolId = 1,
                areaAcademicaId = 1,
                entidadAcademicaId = (int?)null,
            },
            Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty("areaAcademicaId", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Crear_ConRolInvalido_Responde400()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(),
            new
            {
                correo = DatosUnicos.Correo("gmail.com"),
                nombre = "Nombre",
                rolId = 9,
                areaAcademicaId = (int?)null,
                entidadAcademicaId = (int?)null,
            },
            Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Crear_ConCorreoVacio_Responde400ConErrorEnCorreo()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(),
            new { correo = "", nombre = "Nombre", rolId = 1, areaAcademicaId = (int?)null, entidadAcademicaId = (int?)null },
            Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty("correo", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Crear_ConAreaInexistente_Responde400ConErrorEnAreaAcademicaId()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(),
            new
            {
                correo = DatosUnicos.Correo("uv.mx"),
                nombre = "Nombre",
                rolId = 2,
                areaAcademicaId = int.MaxValue,
                entidadAcademicaId = (int?)null,
            },
            Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty("areaAcademicaId", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Crear_ConAreaDadaDeBaja_Responde400ConErrorEnAreaAcademicaId()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearAreaAsync(cliente);
        (await cliente.DeleteAsync(new Uri($"/api/v1/institucional/areas-academicas/{areaId}", UriKind.Relative), Cancelacion))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(),
            new
            {
                correo = DatosUnicos.Correo("uv.mx"),
                nombre = "Nombre",
                rolId = 2,
                areaAcademicaId = areaId,
                entidadAcademicaId = (int?)null,
            },
            Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty("areaAcademicaId", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Crear_ConCorreoExistenteEnMayusculas_Responde409()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var correo = DatosUnicos.Correo("gmail.com");
        await CrearSuperusuarioAsync(cliente, correo);

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(),
            new
            {
                correo = correo.ToUpperInvariant(),
                nombre = "Otro",
                rolId = 1,
                areaAcademicaId = (int?)null,
                entidadAcademicaId = (int?)null,
            },
            Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("Usuario.CorreoDuplicado");
    }

    [Fact]
    public async Task Crear_ConCorreoDeUnaCuentaDadaDeBaja_Responde201()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var correo = DatosUnicos.Correo("gmail.com");
        var id = await CrearSuperusuarioAsync(cliente, correo);
        (await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(),
            new { correo, nombre = "Nuevo", rolId = 1, areaAcademicaId = (int?)null, entidadAcademicaId = (int?)null },
            Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Obtener_Existente_Responde200()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var correo = DatosUnicos.Correo("gmail.com");
        var id = await CrearSuperusuarioAsync(cliente, correo);

        using var respuesta = await cliente.GetAsync(Uri($"/{id}"), Cancelacion);
        var cuenta = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        cuenta.GetProperty("correo").GetString().ShouldBe(correo);
    }

    [Fact]
    public async Task Obtener_Inexistente_Responde404ConCodigo()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.GetAsync(Uri($"/{int.MaxValue}"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problema.GetProperty("codigo").GetString().ShouldBe("Usuario.NoEncontrado");
    }

    [Fact]
    public async Task Obtener_DadaDeBaja_Responde404()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var id = await CrearSuperusuarioAsync(cliente, DatosUnicos.Correo("gmail.com"));
        (await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var respuesta = await cliente.GetAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Modificar_Responde204YPersisteElNombre()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var id = await CrearSuperusuarioAsync(cliente, DatosUnicos.Correo("gmail.com"));

        using var respuesta = await cliente.PutAsJsonAsync(Uri($"/{id}"), new { nombre = "Nombre Modificado" }, Cancelacion);
        using var consulta = await cliente.GetAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Leer(consulta)).GetProperty("nombre").GetString().ShouldBe("Nombre Modificado");
    }

    [Fact]
    public async Task Modificar_ConNombreVacio_Responde400()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var id = await CrearSuperusuarioAsync(cliente, DatosUnicos.Correo("gmail.com"));

        using var respuesta = await cliente.PutAsJsonAsync(Uri($"/{id}"), new { nombre = "" }, Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Modificar_Inexistente_Responde404()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.PutAsJsonAsync(Uri($"/{int.MaxValue}"), new { nombre = "Nombre" }, Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RestablecerContrasena_Responde200YLaAnteriorDejaDeServir()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var correo = DatosUnicos.Correo("gmail.com");
        var id = await CrearSuperusuarioConContrasenaCambiadaAsync(cliente, correo, "Anterior123!");

        using var respuesta = await cliente.PostAsync(Uri($"/{id}/restablecer-contrasena"), content: null, Cancelacion);
        var cuerpo = await Leer(respuesta);
        var temporalNueva = cuerpo.GetProperty("contrasenaTemporal").GetString();

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        temporalNueva.ShouldNotBeNullOrEmpty();

        using var clienteAnonimo = _api.CreateClient();
        using var loginAnterior = await clienteAnonimo.PostAsJsonAsync(
            new Uri(RutaIniciarSesion, UriKind.Relative), new { correo, contrasena = "Anterior123!" }, Cancelacion);
        loginAnterior.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var loginNueva = await clienteAnonimo.PostAsJsonAsync(
            new Uri(RutaIniciarSesion, UriKind.Relative), new { correo, contrasena = temporalNueva }, Cancelacion);
        var sesion = await Leer(loginNueva);
        loginNueva.StatusCode.ShouldBe(HttpStatusCode.OK);
        sesion.GetProperty("usuario").GetProperty("cambioContrasenaPendiente").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task RestablecerContrasena_Inexistente_Responde404()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.PostAsync(Uri($"/{int.MaxValue}/restablecer-contrasena"), content: null, Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RestablecerContrasena_Propia_Responde409()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var idPropio = await IdDeLaSesionAsync(cliente);

        using var respuesta = await cliente.PostAsync(Uri($"/{idPropio}/restablecer-contrasena"), content: null, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("Usuario.RestablecimientoPropio");
    }

    [Fact]
    public async Task RestablecerContrasena_ConCuentaUv_Responde409()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearAreaAsync(cliente);
        var dgaaId = await CrearDgaaAsync(cliente, areaId);

        using var respuesta = await cliente.PostAsync(Uri($"/{dgaaId}/restablecer-contrasena"), content: null, Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("Usuario.RestablecimientoNoAplica");
    }

    [Fact]
    public async Task DarDeBaja_Responde204YSuTokenYaNoIniciaSesion()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var correo = DatosUnicos.Correo("gmail.com");
        var id = await CrearSuperusuarioConContrasenaCambiadaAsync(cliente, correo, "Conocida123!");
        using var clienteDado = await IniciarSesionAsync(correo, "Conocida123!");

        using var respuesta = await cliente.DeleteAsync(Uri($"/{id}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var sesionPosterior = await clienteDado.GetAsync(new Uri(RutaSesion, UriKind.Relative), Cancelacion);
        sesionPosterior.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DarDeBaja_Inexistente_Responde404()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var respuesta = await cliente.DeleteAsync(Uri($"/{int.MaxValue}"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DarDeBaja_Propia_Responde409()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var idPropio = await IdDeLaSesionAsync(cliente);

        using var respuesta = await cliente.DeleteAsync(Uri($"/{idPropio}"), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("Usuario.BajaPropia");
    }

    [Fact]
    public async Task Listar_FiltraPorRolAreaYEntidad()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearAreaAsync(cliente);
        var entidadId = await CrearEntidadAsync(cliente, areaId);
        var dgaaId = await CrearDgaaAsync(cliente, areaId);
        var entidadAcademicaCuentaId = await CrearEntidadAcademicaAsync(cliente, entidadId);

        await VerificaFiltro(cliente, "rolId=2", dgaaId);
        await VerificaFiltro(cliente, $"areaAcademicaId={areaId}", dgaaId, entidadAcademicaCuentaId);
        await VerificaFiltro(cliente, $"entidadAcademicaId={entidadId}", entidadAcademicaCuentaId);
    }

    [Fact]
    public async Task Listar_ConBusquedaIgnoraMayusculasYAcentos()
    {
        // "Modern_Spanish_100_CI_AI" (la colación de la columna, igual que en Institucional) no pliega la ñ a n,
        // solo tildes: por eso el nombre de prueba no lleva ñ (verificado contra SQL Server 2022 real).
        using var cliente = await _api.CrearClienteSuperusuarioAsync();
        var correo = DatosUnicos.Correo("gmail.com");
        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(),
            new { correo, nombre = "María Pérez", rolId = 1, areaAcademicaId = (int?)null, entidadAcademicaId = (int?)null },
            Cancelacion);
        var id = (await Leer(respuesta)).GetProperty("cuenta").GetProperty("id").GetInt32();

        await VerificaFiltro(cliente, $"busqueda={System.Uri.EscapeDataString("maria perez")}", id);
    }

    [Fact]
    public async Task Listar_ConPaginacionInvalida_Responde400()
    {
        using var cliente = await _api.CrearClienteSuperusuarioAsync();

        using var pagina0 = await cliente.GetAsync(Uri("?pagina=0"), Cancelacion);
        using var tamanoExcesivo = await cliente.GetAsync(Uri("?tamanoPagina=101"), Cancelacion);

        pagina0.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        tamanoExcesivo.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoints_SinToken_Responde401()
    {
        using var cliente = _api.CreateClient();

        using var respuesta = await cliente.GetAsync(Uri(), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Endpoints_ConTokenDeDgaa_Responde403ConSinPermiso()
    {
        using var clienteSuperusuario = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearAreaAsync(clienteSuperusuario);
        using var cliente = await _api.CrearClienteDgaaAsync(areaId);

        using var respuesta = await cliente.GetAsync(Uri(), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        problema.GetProperty("codigo").GetString().ShouldBe("Autorizacion.SinPermiso");
    }

    [Fact]
    public async Task Endpoints_ConTokenDeEntidadAcademica_Responde403ConSinPermiso()
    {
        using var clienteSuperusuario = await _api.CrearClienteSuperusuarioAsync();
        var areaId = await CrearAreaAsync(clienteSuperusuario);
        var entidadId = await CrearEntidadAsync(clienteSuperusuario, areaId);
        using var cliente = await _api.CrearClienteEntidadAcademicaAsync(entidadId);

        using var respuesta = await cliente.GetAsync(Uri(), Cancelacion);
        var problema = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        problema.GetProperty("codigo").GetString().ShouldBe("Autorizacion.SinPermiso");
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static Uri Uri(string sufijo = "") => new($"{Ruta}{sufijo}", UriKind.Relative);

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>(Cancelacion);

    private static async Task<int> CrearAreaAsync(HttpClient cliente)
    {
        using var respuesta = await cliente.PostAsJsonAsync(
            new Uri("/api/v1/institucional/areas-academicas", UriKind.Relative),
            new { clave = DatosUnicos.ClaveEntera(), nombre = "Área de prueba", telefono = "2288421700", extension = (string?)null },
            Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await Leer(respuesta)).GetProperty("id").GetInt32();
    }

    private static async Task<int> CrearEntidadAsync(HttpClient cliente, int areaAcademicaId)
    {
        using var respuesta = await cliente.PostAsJsonAsync(
            new Uri("/api/v1/institucional/entidades-academicas", UriKind.Relative),
            new
            {
                clave = DatosUnicos.ClaveAlfanumerica(),
                nombre = "Entidad de prueba",
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

        return (await Leer(respuesta)).GetProperty("id").GetInt32();
    }

    private static async Task<int> CrearDgaaAsync(HttpClient cliente, int areaAcademicaId)
    {
        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(),
            new
            {
                correo = DatosUnicos.Correo("uv.mx"),
                nombre = "DGAA de prueba",
                rolId = 2,
                areaAcademicaId,
                entidadAcademicaId = (int?)null,
            },
            Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await Leer(respuesta)).GetProperty("cuenta").GetProperty("id").GetInt32();
    }

    private static async Task<int> CrearEntidadAcademicaAsync(HttpClient cliente, int entidadAcademicaId)
    {
        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(),
            new
            {
                correo = DatosUnicos.Correo("uv.mx"),
                nombre = "Entidad de prueba",
                rolId = 3,
                areaAcademicaId = (int?)null,
                entidadAcademicaId,
            },
            Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await Leer(respuesta)).GetProperty("cuenta").GetProperty("id").GetInt32();
    }

    private static async Task<int> CrearSuperusuarioAsync(HttpClient cliente, string correo)
    {
        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(),
            new { correo, nombre = "Superusuario de prueba", rolId = 1, areaAcademicaId = (int?)null, entidadAcademicaId = (int?)null },
            Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await Leer(respuesta)).GetProperty("cuenta").GetProperty("id").GetInt32();
    }

    private async Task<int> CrearSuperusuarioConContrasenaCambiadaAsync(HttpClient cliente, string correo, string contrasenaNueva)
    {
        using var respuesta = await cliente.PostAsJsonAsync(
            Uri(),
            new { correo, nombre = "Superusuario de prueba", rolId = 1, areaAcademicaId = (int?)null, entidadAcademicaId = (int?)null },
            Cancelacion);
        var creada = await Leer(respuesta);
        var temporal = creada.GetProperty("contrasenaTemporal").GetString();
        var id = creada.GetProperty("cuenta").GetProperty("id").GetInt32();

        using var clienteAnonimo = _api.CreateClient();
        using var login = await clienteAnonimo.PostAsJsonAsync(
            new Uri(RutaIniciarSesion, UriKind.Relative), new { correo, contrasena = temporal }, Cancelacion);
        var sesion = await Leer(login);

        clienteAnonimo.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", sesion.GetProperty("token").GetString());
        using var cambio = await clienteAnonimo.PostAsJsonAsync(
            new Uri("/api/v1/usuarios/sesion/cambiar-contrasena", UriKind.Relative),
            new { contrasenaActual = temporal, contrasenaNueva },
            Cancelacion);
        cambio.StatusCode.ShouldBe(HttpStatusCode.OK);

        return id;
    }

    private async Task<HttpClient> IniciarSesionAsync(string correo, string contrasena)
    {
        var cliente = _api.CreateClient();
        using var respuesta = await cliente.PostAsJsonAsync(
            new Uri(RutaIniciarSesion, UriKind.Relative), new { correo, contrasena }, Cancelacion);
        var sesion = await Leer(respuesta);
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", sesion.GetProperty("token").GetString());
        return cliente;
    }

    private static async Task<int> IdDeLaSesionAsync(HttpClient cliente)
    {
        using var respuesta = await cliente.GetAsync(new Uri(RutaSesion, UriKind.Relative), Cancelacion);
        return (await Leer(respuesta)).GetProperty("id").GetInt32();
    }

    private static async Task VerificaFiltro(HttpClient cliente, string queryString, params int[] idsEsperados)
    {
        using var respuesta = await cliente.GetAsync(Uri($"?{queryString}"), Cancelacion);
        var pagina = await Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var ids = pagina.GetProperty("elementos").EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToList();
        foreach (var idEsperado in idsEsperados)
        {
            ids.ShouldContain(idEsperado);
        }
    }
}
