using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Sgpla.IntegrationTests.Infraestructura;
using Sgpla.IntegrationTests.OfertaEducativa;

namespace Sgpla.IntegrationTests.SolicitudesApertura;

public sealed class SolicitudAperturaEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private const string Ruta = "/api/v1/solicitudes-apertura/solicitudes";

    private readonly SgplaApiFactory _api = new(sqlServer);

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Crear_ConDatosValidos_Devuelve201RecursoDerivadoYOficioIdentico()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var pdf = "%PDF-1.7\ncontenido exacto"u8.ToArray();
        using var formulario = escenario.CrearFormulario(seccion: " a1 ", bytes: pdf);

        using var respuesta = await escenario.Entidad.PostAsync(Uri(), formulario, Cancelacion);
        var creada = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
        respuesta.Headers.Location.ShouldNotBeNull().AbsolutePath.ShouldBe($"{Ruta}/{creada.GetProperty("id").GetInt32()}");
        creada.GetProperty("estado").GetString().ShouldBe("PENDIENTE");
        creada.GetProperty("seccion").GetString().ShouldBe("A1");
        creada.GetProperty("cantidadEstudiantes").GetInt32().ShouldBe(20);
        creada.GetProperty("justificacion").GetString().ShouldBe("Solicitud de apertura.");
        creada.GetProperty("experienciaEducativa").GetProperty("id").GetInt32().ShouldBe(escenario.ExperienciaId);
        creada.GetProperty("experienciaEducativa").GetProperty("materia").GetString().ShouldBe("ENSO");
        creada.GetProperty("experienciaEducativa").GetProperty("curso").GetString().ShouldBe("00001");
        creada.GetProperty("experienciaEducativa").GetProperty("cupoMinimo").GetInt32().ShouldBe(10);
        creada.GetProperty("experienciaEducativa").GetProperty("cupoMaximo").GetInt32().ShouldBe(40);
        creada.GetProperty("experienciaEducativa").GetProperty("planEstudiosId").GetInt32().ShouldBe(escenario.PlanId);
        creada.GetProperty("experienciaEducativa").GetProperty("programaEducativoId").GetInt32().ShouldBe(escenario.Oferta.ProgramaId);
        creada.GetProperty("experienciaEducativa").GetProperty("entidadAcademicaId").GetInt32().ShouldBe(escenario.Oferta.EntidadId);
        creada.GetProperty("experienciaEducativa").GetProperty("modalidad").GetString().ShouldBe("Escolarizada");
        creada.GetProperty("periodoEscolar").GetProperty("id").GetInt32().ShouldBe(escenario.PeriodoSiguienteId);
        creada.GetProperty("oficio").GetProperty("nombre").GetString().ShouldBe("oficio.pdf");
        creada.GetProperty("oficio").GetProperty("tamano").GetInt64().ShouldBe(pdf.LongLength);

        var id = creada.GetProperty("id").GetInt32();
        using var descarga = await escenario.Entidad.GetAsync(Uri($"/{id}/oficio"), Cancelacion);
        descarga.StatusCode.ShouldBe(HttpStatusCode.OK);
        descarga.Content.Headers.ContentType?.MediaType.ShouldBe("application/pdf");
        (await descarga.Content.ReadAsByteArrayAsync(Cancelacion)).ShouldBe(pdf);
        (await ObtenerChecksumAsync(id)).ShouldBe(SHA256.HashData(pdf));
    }

    [Theory]
    [InlineData("", 20, "Solicitud válida.", true, "application/pdf", "%PDF-1234", "seccion", "SolicitudApertura.SeccionVacia")]
    [InlineData("A 1", 20, "Solicitud válida.", true, "application/pdf", "%PDF-1234", "seccion", "SolicitudApertura.SeccionFormatoInvalido")]
    [InlineData("A1", 0, "Solicitud válida.", true, "application/pdf", "%PDF-1234", "cantidadEstudiantes", "SolicitudApertura.CantidadNoPositiva")]
    [InlineData("A1", 20, " ", true, "application/pdf", "%PDF-1234", "justificacion", "SolicitudApertura.JustificacionVacia")]
    [InlineData("A1", 20, "Solicitud válida.", false, "application/pdf", "%PDF-1234", "oficio", "ArchivoSolicitudApertura.Obligatorio")]
    [InlineData("A1", 20, "Solicitud válida.", true, "text/plain", "%PDF-1234", "oficio", "ArchivoSolicitudApertura.NoEsPdf")]
    [InlineData("A1", 20, "Solicitud válida.", true, "application/pdf", "texto inválido", "oficio", "ArchivoSolicitudApertura.NoEsPdf")]
    public async Task Crear_ConDatosInvalidos_Devuelve400ConCodigoYCampo(
        string seccion,
        int cantidad,
        string justificacion,
        bool agregarOficio,
        string mime,
        string contenido,
        string campo,
        string codigo)
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        using var formulario = escenario.CrearFormulario(
            seccion: seccion,
            cantidad: cantidad,
            justificacion: justificacion,
            agregarOficio: agregarOficio,
            bytes: Encoding.UTF8.GetBytes(contenido),
            tipoContenido: mime);

        using var respuesta = await escenario.Entidad.PostAsync(Uri(), formulario, Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, campo, codigo);
    }

    [Fact]
    public async Task Crear_ConOficioVacio_Devuelve400EnOficio()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        using var formulario = escenario.CrearFormulario(bytes: []);

        using var respuesta = await escenario.Entidad.PostAsync(Uri(), formulario, Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "oficio", "ArchivoSolicitudApertura.Vacio");
    }

    [Fact]
    public async Task Crear_ConOficioMayorAlMaximo_Devuelve400EnOficio()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        using var formulario = escenario.CrearFormulario(bytes: new byte[10_485_761]);

        using var respuesta = await escenario.Entidad.PostAsync(Uri(), formulario, Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "oficio", "ArchivoSolicitudApertura.DemasiadoGrande");
    }

    [Fact]
    public async Task Crear_ConExperienciaDeOtraEntidad_Devuelve400EnExperienciaEducativaId()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        using var otraOferta = await EscenarioOferta.CrearAsync(_api, escenario.Superusuario);
        var experienciaAjena = await CrearExperienciaAsync(otraOferta);
        using var formulario = escenario.CrearFormulario(experienciaId: experienciaAjena);

        using var respuesta = await escenario.Entidad.PostAsync(Uri(), formulario, Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "experienciaEducativaId", "SolicitudApertura.ExperienciaEducativaInvalida");
    }

    [Fact]
    public async Task Crear_ConPeriodoInexistente_Devuelve400EnPeriodoEscolarId()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        using var formulario = escenario.CrearFormulario(periodoId: 999_999_999);

        using var respuesta = await escenario.Entidad.PostAsync(Uri(), formulario, Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "periodoEscolarId", "SolicitudApertura.PeriodoEscolarInvalido");
    }

    [Fact]
    public async Task Crear_ConPeriodoActual_Devuelve409PeriodoNoAbierto()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        using var formulario = escenario.CrearFormulario(periodoId: escenario.PeriodoActualId);

        using var respuesta = await escenario.Entidad.PostAsync(Uri(), formulario, Cancelacion);
        var problema = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("SolicitudApertura.PeriodoNoAbierto");
    }

    [Fact]
    public async Task Crear_ConPeriodoSiguienteNoConfiguradoActivo_Devuelve409PeriodosNoDisponibles()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        using var host = _api.WithWebHostBuilder(builder => builder.UseSetting("SolicitudesApertura:PeriodoSiguiente", "999802"));
        using var cliente = host.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = escenario.Entidad.DefaultRequestHeaders.Authorization;
        using var formulario = escenario.CrearFormulario();

        using var respuesta = await cliente.PostAsync(Uri(), formulario, Cancelacion);
        var problema = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("SolicitudApertura.PeriodosNoDisponibles");
    }

    [Theory]
    [InlineData(9)]
    [InlineData(41)]
    public async Task Crear_ConCantidadFueraDeCupos_Devuelve409(int cantidad)
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        using var formulario = escenario.CrearFormulario(cantidad: cantidad);

        using var respuesta = await escenario.Entidad.PostAsync(Uri(), formulario, Cancelacion);
        var problema = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("SolicitudApertura.CantidadFueraDeCupos");
    }

    [Fact]
    public async Task Crear_ConExperienciaSinCupos_Devuelve201()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api, cupoMinimo: null, cupoMaximo: null);

        await escenario.CrearSolicitudAsync(cantidad: 80);
    }

    [Fact]
    public async Task Crear_ConSeccionPendienteDuplicada_Devuelve409YNoDuplica()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        const string seccion = "A1";
        await DatosSolicitudesSql.InsertarAsync(
            sqlServer.CadenaConexion, escenario.ExperienciaId, escenario.PeriodoSiguienteId, "PENDIENTE", seccion);
        using var formulario = escenario.CrearFormulario(seccion: seccion.ToLowerInvariant());

        using var respuesta = await escenario.Entidad.PostAsync(Uri(), formulario, Cancelacion);
        var problema = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe("SolicitudApertura.SeccionDuplicada");
    }

    [Fact]
    public async Task Crear_ConSeccionAnteriorCancelada_Devuelve201()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        const string seccion = "A1";
        await DatosSolicitudesSql.InsertarAsync(
            sqlServer.CadenaConexion, escenario.ExperienciaId, escenario.PeriodoSiguienteId, "CANCELADA", seccion);

        await escenario.CrearSolicitudAsync(seccion: seccion);
    }

    [Fact]
    public async Task Crear_SoloPermiteEntidadAcademica()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        using var dgaaFormulario = escenario.CrearFormulario();
        using var superusuarioFormulario = escenario.CrearFormulario();

        using var dgaa = await escenario.Oferta.Dgaa.PostAsync(Uri(), dgaaFormulario, Cancelacion);
        using var superusuario = await escenario.Superusuario.PostAsync(Uri(), superusuarioFormulario, Cancelacion);

        dgaa.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        superusuario.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListarYObtener_FiltraPorAmbitoYFiltrosDeclarados()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, creada) = await escenario.CrearSolicitudAsync(seccion: "A1");

        using var listar = await escenario.Entidad.GetAsync(
            Uri($"?experienciaEducativaId={escenario.ExperienciaId}&periodoEscolarId={escenario.PeriodoSiguienteId}&entidadAcademicaId={escenario.Oferta.EntidadId}&estado=pendiente"),
            Cancelacion);
        var pagina = await EscenarioOferta.Leer(listar);

        listar.StatusCode.ShouldBe(HttpStatusCode.OK);
        pagina.GetProperty("elementos").EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ShouldBe([id]);
        pagina.GetProperty("total").GetInt32().ShouldBe(1);

        using var obtener = await escenario.Entidad.GetAsync(Uri($"/{id}"), Cancelacion);
        obtener.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await EscenarioOferta.Leer(obtener)).GetProperty("id").GetInt32().ShouldBe(id);

        var filtros = new[]
        {
            $"?experienciaEducativaId={escenario.ExperienciaId}",
            $"?periodoEscolarId={escenario.PeriodoSiguienteId}",
            $"?entidadAcademicaId={escenario.Oferta.EntidadId}",
            "?estado=PENDIENTE",
        };
        foreach (var filtro in filtros)
        {
            using var filtrado = await escenario.Entidad.GetAsync(Uri(filtro), Cancelacion);
            filtrado.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await EscenarioOferta.Leer(filtrado)).GetProperty("elementos").EnumerateArray()
                .Select(e => e.GetProperty("id").GetInt32()).ShouldContain(id);
        }

        creada.GetProperty("estado").GetString().ShouldBe("PENDIENTE");
    }

    [Theory]
    [InlineData("?estado=OTRO", "estado")]
    [InlineData("?tamanoPagina=101", "tamanoPagina")]
    public async Task Listar_ConFiltroInvalido_Devuelve400(string query, string campo)
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);

        using var respuesta = await escenario.Entidad.GetAsync(Uri(query), Cancelacion);
        var problema = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("errors").TryGetProperty(campo, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task LeerYDescargar_SinAmbitoDevuelve404YSuperusuarioNoDescarga()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        using var otraOferta = await EscenarioOferta.CrearAsync(_api, escenario.Superusuario);
        using var otraEntidad = await _api.CrearClienteEntidadAcademicaAsync(otraOferta.EntidadId);
        var otroPlan = await EscenarioOferta.CrearPlanAsync(
            otraOferta.Dgaa,
            otraOferta.ProgramaId,
            EscenarioOferta.CodigoDePlan(),
            [EscenarioOferta.Experiencia(cupoMinimo: 10, cupoMaximo: 40)]);
        var otraExperiencia = await ObtenerExperienciaAsync(otraOferta.Dgaa, otroPlan);
        var (otroId, _) = await CrearSolicitudAjenaAsync(escenario, otraEntidad, otraExperiencia);

        using var obtener = await escenario.Entidad.GetAsync(Uri($"/{otroId}"), Cancelacion);
        using var descargarFueraDeAmbito = await escenario.Entidad.GetAsync(Uri($"/{otroId}/oficio"), Cancelacion);
        using var obtenerComoDgaaFueraDeArea = await escenario.Oferta.Dgaa.GetAsync(Uri($"/{otroId}"), Cancelacion);
        using var obtenerComoDgaaDeAreaAjena = await otraOferta.Dgaa.GetAsync(Uri($"/{otroId}"), Cancelacion);
        using var descargarComoSuperusuario = await escenario.Superusuario.GetAsync(Uri($"/{otroId}/oficio"), Cancelacion);
        using var obtenerComoSuperusuario = await escenario.Superusuario.GetAsync(Uri($"/{otroId}"), Cancelacion);
        using var listarEntidadFueraDeAmbito = await escenario.Entidad.GetAsync(
            Uri($"?experienciaEducativaId={otraExperiencia}"), Cancelacion);
        using var listarSuperusuario = await escenario.Superusuario.GetAsync(
            Uri($"?experienciaEducativaId={otraExperiencia}"), Cancelacion);

        await VerificaNoEncontradoAsync(obtener);
        await VerificaNoEncontradoAsync(descargarFueraDeAmbito);
        await VerificaNoEncontradoAsync(obtenerComoDgaaFueraDeArea);
        obtenerComoDgaaDeAreaAjena.StatusCode.ShouldBe(HttpStatusCode.OK);
        descargarComoSuperusuario.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        obtenerComoSuperusuario.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await EscenarioOferta.Leer(listarEntidadFueraDeAmbito)).GetProperty("elementos").GetArrayLength().ShouldBe(0);
        (await EscenarioOferta.Leer(listarSuperusuario)).GetProperty("elementos").EnumerateArray()
            .Select(e => e.GetProperty("id").GetInt32()).ShouldBe([otroId]);
    }

    [Fact]
    public async Task DescargarOficio_DgaaPuedeDescargarDentroDelArea()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync();

        using var descarga = await escenario.Oferta.Dgaa.GetAsync(Uri($"/{id}/oficio"), Cancelacion);

        descarga.StatusCode.ShouldBe(HttpStatusCode.OK);
        descarga.Content.Headers.ContentType?.MediaType.ShouldBe("application/pdf");
        descarga.Content.Headers.ContentDisposition.ShouldNotBeNull();
        (descarga.Content.Headers.ContentDisposition.FileNameStar ?? descarga.Content.Headers.ContentDisposition.FileName)
            ?.Trim('"').ShouldBe("oficio.pdf");
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static Uri Uri(string sufijo = "") => new($"{Ruta}{sufijo}", UriKind.Relative);

    private async Task<byte[]> ObtenerChecksumAsync(int solicitudId)
    {
        await using var conexion = new SqlConnection(sqlServer.CadenaConexion);
        await conexion.OpenAsync(Cancelacion);
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            SELECT archivo.checksum_sha256
            FROM academico.solicitud_apertura AS solicitud
            JOIN academico.archivo_solicitud_apertura AS archivo ON archivo.id = solicitud.oficio_respaldo_id
            WHERE solicitud.id = @id
            """;
        comando.Parameters.AddWithValue("@id", solicitudId);
        return (byte[])(await comando.ExecuteScalarAsync(Cancelacion))!;
    }

    private static async Task<int> CrearExperienciaAsync(EscenarioOferta oferta)
    {
        var planId = await EscenarioOferta.CrearPlanAsync(
            oferta.Dgaa,
            oferta.ProgramaId,
            EscenarioOferta.CodigoDePlan(),
            [EscenarioOferta.Experiencia(cupoMinimo: 10, cupoMaximo: 40)]);
        return await ObtenerExperienciaAsync(oferta.Dgaa, planId);
    }

    private static async Task<int> ObtenerExperienciaAsync(HttpClient cliente, int planId)
    {
        using var respuesta = await cliente.GetAsync(
            new Uri($"/api/v1/oferta-educativa/planes-estudio/{planId}/experiencias-educativas", UriKind.Relative),
            Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await EscenarioOferta.Leer(respuesta)).EnumerateArray().ShouldHaveSingleItem().GetProperty("id").GetInt32();
    }

    private static async Task<(int Id, JsonElement Respuesta)> CrearSolicitudAjenaAsync(
        EscenarioSolicitud baseEscenario,
        HttpClient entidadAjena,
        int experienciaId)
    {
        using var formulario = baseEscenario.CrearFormulario(experienciaId: experienciaId);
        using var respuesta = await entidadAjena.PostAsync(Uri(), formulario, Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var cuerpo = await EscenarioOferta.Leer(respuesta);
        return (cuerpo.GetProperty("id").GetInt32(), cuerpo);
    }

    private static async Task VerificaErrorDeCampoAsync(HttpResponseMessage respuesta, string campo, string codigo)
    {
        var problema = await EscenarioOferta.Leer(respuesta);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.GetProperty("codigo").GetString().ShouldBe(codigo);
        problema.GetProperty("errors").TryGetProperty(campo, out _).ShouldBeTrue();
    }

    private static async Task VerificaNoEncontradoAsync(HttpResponseMessage respuesta)
    {
        var problema = await EscenarioOferta.Leer(respuesta);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problema.GetProperty("codigo").GetString().ShouldBe("SolicitudApertura.NoEncontrada");
    }
}
