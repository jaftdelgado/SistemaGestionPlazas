using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Sgpla.BuildingBlocks.Application;
using Sgpla.IntegrationTests.Infraestructura;
using Sgpla.IntegrationTests.OfertaEducativa;

namespace Sgpla.IntegrationTests.SolicitudesApertura;

public sealed class SolicitudAperturaEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private const string Ruta = "/api/v1/solicitudes-apertura/solicitudes";
    private const string PatronInstanteUtc = @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$";

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
        creada.GetProperty("experienciaEducativa").GetProperty("planEstudiosCodigo").GetString().ShouldBe(escenario.PlanCodigo);
        creada.GetProperty("experienciaEducativa").GetProperty("programaEducativoNombre").GetString()
            .ShouldBe(escenario.Oferta.ProgramaNombre);
        creada.GetProperty("experienciaEducativa").GetProperty("entidadAcademicaClave").GetString()
            .ShouldBe(escenario.Oferta.EntidadClave);
        creada.GetProperty("experienciaEducativa").GetProperty("entidadAcademicaNombre").GetString()
            .ShouldBe(escenario.Oferta.EntidadNombre);
        creada.GetProperty("periodoEscolar").GetProperty("id").GetInt32().ShouldBe(escenario.PeriodoSiguienteId);
        creada.GetProperty("periodoEscolar").GetProperty("clave").GetString().ShouldBe(SqlServerFixture.ClavePeriodoSiguiente);
        creada.GetProperty("oficio").GetProperty("nombre").GetString().ShouldBe("oficio.pdf");
        creada.GetProperty("oficio").GetProperty("tamano").GetInt64().ShouldBe(pdf.LongLength);
        creada.GetProperty("actualizadaEn").ValueKind.ShouldBe(JsonValueKind.Null);
        creada.GetProperty("resueltaEn").ValueKind.ShouldBe(JsonValueKind.Null);
        creada.GetProperty("comentariosResolucion").ValueKind.ShouldBe(JsonValueKind.Null);
        creada.GetProperty("canceladaEn").ValueKind.ShouldBe(JsonValueKind.Null);
        creada.GetProperty("motivoCancelacion").ValueKind.ShouldBe(JsonValueKind.Null);

        var id = creada.GetProperty("id").GetInt32();
        var creadaEn = creada.GetProperty("creadaEn").GetString().ShouldNotBeNull();
        creadaEn.ShouldMatch(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$");
        creada.GetProperty("oficio").GetProperty("cargadoEn").GetString().ShouldBe(creadaEn);
        using var obtenida = await escenario.Entidad.GetAsync(Uri($"/{id}"), Cancelacion);
        obtenida.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await EscenarioOferta.Leer(obtenida)).GetProperty("creadaEn").GetString().ShouldBe(creadaEn);

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
    public async Task Crear_ConExperienciaDadaDeBaja_Devuelve400EnExperienciaEducativaId()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        using var baja = await escenario.Oferta.Dgaa.DeleteAsync(
            new Uri($"/api/v1/oferta-educativa/experiencias-educativas/{escenario.ExperienciaId}", UriKind.Relative),
            Cancelacion);
        baja.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var formulario = escenario.CrearFormulario();

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
    public async Task Crear_ConPeriodoActivoQueNoEsElSiguiente_Devuelve409PeriodoNoAbierto()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var periodoPropioId = await EscenarioOferta.CrearPeriodoAsync(escenario.Superusuario);
        using var formulario = escenario.CrearFormulario(periodoId: periodoPropioId);

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
    public async Task Listar_OrdenaPorCreadaEnDescendenteYPorIdDescendenteEnEmpate()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var instante = new DateTime(2026, 10, 1, 15, 4, 5, DateTimeKind.Utc);
        var a = await InsertarAsync(escenario, "PENDIENTE", "A1", instante);
        var b = await InsertarAsync(escenario, "PENDIENTE", "A2", instante.AddSeconds(-1));
        var c = await InsertarAsync(escenario, "PENDIENTE", "A3", instante);

        var ids = await ListarIdsAsync(escenario.Entidad, $"?experienciaEducativaId={escenario.ExperienciaId}");

        ids.ShouldBe([c, a, b]);
        using var obtener = await escenario.Entidad.GetAsync(Uri($"/{a}"), Cancelacion);
        obtener.StatusCode.ShouldBe(HttpStatusCode.OK);
        var solicitud = await EscenarioOferta.Leer(obtener);
        solicitud.GetProperty("creadaEn").GetString().ShouldBe("2026-10-01T15:04:05Z");
        solicitud.GetProperty("oficio").GetProperty("cargadoEn").GetString().ShouldBe("2026-10-01T15:04:05Z");
    }

    [Fact]
    public async Task ListarYObtener_FiltraPorAmbitoYFiltrosDeclarados()
    {
        using var escenario1 = await EscenarioSolicitud.CrearAsync(_api);
        using var escenario2 = await EscenarioSolicitud.CrearAsync(_api);
        var periodoPropioId = await EscenarioOferta.CrearPeriodoAsync(escenario1.Superusuario);
        var planId = await EscenarioOferta.CrearPlanAsync(
            escenario1.Oferta.Dgaa,
            escenario1.Oferta.ProgramaId,
            EscenarioOferta.CodigoDePlan(),
            [EscenarioOferta.Experiencia(cupoMinimo: 10, cupoMaximo: 40)]);
        var experiencia2Id = await ObtenerExperienciaAsync(escenario1.Oferta.Dgaa, planId);
        var siguienteId = escenario1.PeriodoSiguienteId;
        var s1 = await DatosSolicitudesSql.InsertarAsync(
            sqlServer.CadenaConexion, escenario1.ExperienciaId, siguienteId, "PENDIENTE", "B1");
        var s2 = await DatosSolicitudesSql.InsertarAsync(
            sqlServer.CadenaConexion, escenario1.ExperienciaId, siguienteId, "CANCELADA", "B2");
        var s3 = await DatosSolicitudesSql.InsertarAsync(
            sqlServer.CadenaConexion, escenario1.ExperienciaId, periodoPropioId, "PENDIENTE", "B3");
        var s4 = await DatosSolicitudesSql.InsertarAsync(
            sqlServer.CadenaConexion, experiencia2Id, siguienteId, "PENDIENTE", "B4");
        var s5 = await DatosSolicitudesSql.InsertarAsync(
            sqlServer.CadenaConexion, escenario2.ExperienciaId, siguienteId, "PENDIENTE", "B5");
        var entidad1 = escenario1.Oferta.EntidadId;
        var entidad2 = escenario2.Oferta.EntidadId;

        (await ListarIdsAsync(escenario1.Entidad, "?estado=cancelada")).ShouldBe([s2], ignoreOrder: true);
        (await ListarIdsAsync(escenario1.Entidad, "?estado=PENDIENTE")).ShouldBe([s1, s3, s4], ignoreOrder: true);
        (await ListarIdsAsync(escenario1.Entidad, $"?periodoEscolarId={siguienteId}")).ShouldBe([s1, s2, s4], ignoreOrder: true);
        (await ListarIdsAsync(escenario1.Entidad, $"?experienciaEducativaId={experiencia2Id}")).ShouldBe([s4]);
        (await ListarIdsAsync(escenario1.Entidad, string.Empty)).ShouldBe([s1, s2, s3, s4], ignoreOrder: true);
        (await ListarIdsAsync(escenario1.Oferta.Dgaa, string.Empty)).ShouldBe([s1, s2, s3, s4], ignoreOrder: true);
        (await ListarIdsAsync(escenario1.Superusuario, $"?entidadAcademicaId={entidad1}")).ShouldBe([s1, s2, s3, s4], ignoreOrder: true);
        (await ListarIdsAsync(escenario1.Superusuario, $"?entidadAcademicaId={entidad2}")).ShouldBe([s5]);
        await VerificaListadoVacioAsync(escenario1.Entidad, $"?entidadAcademicaId={entidad2}");
        await VerificaListadoVacioAsync(escenario1.Oferta.Dgaa, $"?entidadAcademicaId={entidad2}");

        using var obtener = await escenario1.Entidad.GetAsync(Uri($"/{s1}"), Cancelacion);
        obtener.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await EscenarioOferta.Leer(obtener)).GetProperty("id").GetInt32().ShouldBe(s1);
    }

    [Theory]
    [InlineData("?estado=OTRO", "estado")]
    [InlineData("?tamanoPagina=101", "tamanoPagina")]
    public async Task Listar_ConFiltroInvalido_Devuelve400(string query, string campo)
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);

        using var respuesta = await escenario.Entidad.GetAsync(Uri(query), Cancelacion);
        var contenido = await respuesta.Content.ReadAsStringAsync(Cancelacion);
        var problema = await EscenarioOferta.Leer(respuesta);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problema.TryGetProperty("errors", out var errores).ShouldBeTrue(contenido);
        errores.TryGetProperty(campo, out _).ShouldBeTrue(contenido);
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
        var contenidoListadoSuperusuario = await listarSuperusuario.Content.ReadAsStringAsync(Cancelacion);

        await VerificaNoEncontradoAsync(obtener);
        await VerificaNoEncontradoAsync(descargarFueraDeAmbito);
        await VerificaNoEncontradoAsync(obtenerComoDgaaFueraDeArea);
        obtenerComoDgaaDeAreaAjena.StatusCode.ShouldBe(HttpStatusCode.OK);
        descargarComoSuperusuario.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        obtenerComoSuperusuario.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await EscenarioOferta.Leer(listarEntidadFueraDeAmbito)).GetProperty("elementos").GetArrayLength().ShouldBe(0);
        listarSuperusuario.StatusCode.ShouldBe(HttpStatusCode.OK, contenidoListadoSuperusuario);
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

    [Fact]
    public async Task Modificar_SinOficio_Devuelve204YConservaElOficio()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, creada) = await escenario.CrearSolicitudAsync(cantidad: 20);
        var oficioAntes = await LeerOficioAsync(id);
        using var formulario = EscenarioSolicitud.CrearFormularioModificacion(25, "  Justificación nueva.  ");

        using var respuesta = await escenario.Entidad.PutAsync(Uri($"/{id}"), formulario, Cancelacion);
        var modificada = await ObtenerAsync(escenario.Entidad, id);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        modificada.GetProperty("cantidadEstudiantes").GetInt32().ShouldBe(25);
        modificada.GetProperty("justificacion").GetString().ShouldBe("Justificación nueva.");
        modificada.GetProperty("actualizadaEn").GetString().ShouldNotBeNull().ShouldMatch(PatronInstanteUtc);
        modificada.GetProperty("actualizadaPorUsuarioId").ValueKind.ShouldBe(JsonValueKind.Number);
        modificada.GetProperty("estado").GetString().ShouldBe("PENDIENTE");
        modificada.GetProperty("oficio").GetRawText().ShouldBe(creada.GetProperty("oficio").GetRawText());
        (await LeerOficioAsync(id)).ShouldBe(oficioAntes);
    }

    [Fact]
    public async Task Modificar_ConOficio_Devuelve204YElOficioAnteriorDejaDeExistir()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync();
        var (archivoAnteriorId, claveAnterior) = await LeerOficioAsync(id);
        var nuevo = "%PDF-1.7\nOficio de reemplazo"u8.ToArray();
        using var formulario = EscenarioSolicitud.CrearFormularioModificacion(agregarOficio: true, bytes: nuevo);

        using var respuesta = await escenario.Entidad.PutAsync(Uri($"/{id}"), formulario, Cancelacion);
        var modificada = await ObtenerAsync(escenario.Entidad, id);
        using var descarga = await escenario.Entidad.GetAsync(Uri($"/{id}/oficio"), Cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ExisteArchivoAsync(archivoAnteriorId)).ShouldBeFalse();
        var almacenamiento = _api.Services.GetRequiredService<IAlmacenamientoArchivos>();
        (await almacenamiento.AbrirAsync(claveAnterior, Cancelacion)).ShouldBeNull();
        modificada.GetProperty("oficio").GetProperty("nombre").GetString().ShouldBe("nuevo.pdf");
        modificada.GetProperty("oficio").GetProperty("tamano").GetInt64().ShouldBe(nuevo.LongLength);
        descarga.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await descarga.Content.ReadAsByteArrayAsync(Cancelacion)).ShouldBe(nuevo);
        (await LeerOficioAsync(id)).Id.ShouldNotBe(archivoAnteriorId);
    }

    [Theory]
    [InlineData(0, "Justificación.", false, "application/pdf", "%PDF-1234", "cantidadEstudiantes", "SolicitudApertura.CantidadNoPositiva")]
    [InlineData(25, " ", false, "application/pdf", "%PDF-1234", "justificacion", "SolicitudApertura.JustificacionVacia")]
    [InlineData(25, "Justificación.", true, "application/pdf", "", "oficio", "ArchivoSolicitudApertura.Vacio")]
    [InlineData(25, "Justificación.", true, "text/plain", "%PDF-1234", "oficio", "ArchivoSolicitudApertura.NoEsPdf")]
    [InlineData(25, "Justificación.", true, "application/pdf", "texto inválido", "oficio", "ArchivoSolicitudApertura.NoEsPdf")]
    public async Task Modificar_ConDatosInvalidos_Devuelve400ConCodigoYCampo(
        int cantidad,
        string justificacion,
        bool agregarOficio,
        string mime,
        string contenido,
        string campo,
        string codigo)
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync();
        using var formulario = EscenarioSolicitud.CrearFormularioModificacion(
            cantidad, justificacion, agregarOficio, Encoding.UTF8.GetBytes(contenido), tipoContenido: mime);

        using var respuesta = await escenario.Entidad.PutAsync(Uri($"/{id}"), formulario, Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, campo, codigo);
    }

    [Fact]
    public async Task Modificar_ConJustificacionDeMasDeDosMilCaracteres_Devuelve400EnJustificacion()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync();
        using var formulario = EscenarioSolicitud.CrearFormularioModificacion(justificacion: new string('x', 2001));

        using var respuesta = await escenario.Entidad.PutAsync(Uri($"/{id}"), formulario, Cancelacion);

        await VerificaErrorDeCampoAsync(respuesta, "justificacion", "SolicitudApertura.JustificacionDemasiadoLarga");
    }

    [Fact]
    public async Task Modificar_ConCantidadFueraDeCupos_Devuelve409()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync();
        using var formulario = EscenarioSolicitud.CrearFormularioModificacion(41);

        using var respuesta = await escenario.Entidad.PutAsync(Uri($"/{id}"), formulario, Cancelacion);

        await VerificaConflictoAsync(respuesta, "SolicitudApertura.CantidadFueraDeCupos");
    }

    [Fact]
    public async Task Modificar_FueraDePendiente_Devuelve409NoPendiente()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var id = await InsertarTerminalAsync(escenario, "ACEPTADA");
        using var formulario = EscenarioSolicitud.CrearFormularioModificacion();

        using var respuesta = await escenario.Entidad.PutAsync(Uri($"/{id}"), formulario, Cancelacion);

        await VerificaConflictoAsync(respuesta, "SolicitudApertura.NoPendiente");
    }

    [Fact]
    public async Task Modificar_ConSolicitudDeOtraEntidad_Devuelve404()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        using var otro = await EscenarioSolicitud.CrearAsync(_api);
        var (idAjeno, _) = await otro.CrearSolicitudAsync();
        using var formulario = EscenarioSolicitud.CrearFormularioModificacion();

        using var respuesta = await escenario.Entidad.PutAsync(Uri($"/{idAjeno}"), formulario, Cancelacion);

        await VerificaNoEncontradoAsync(respuesta);
        (await ObtenerAsync(otro.Entidad, idAjeno)).GetProperty("actualizadaEn").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Modificar_SoloPermiteEntidadAcademica()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync();
        using var formularioDgaa = EscenarioSolicitud.CrearFormularioModificacion();
        using var formularioSuperusuario = EscenarioSolicitud.CrearFormularioModificacion();

        using var dgaa = await escenario.Oferta.Dgaa.PutAsync(Uri($"/{id}"), formularioDgaa, Cancelacion);
        using var superusuario = await escenario.Superusuario.PutAsync(Uri($"/{id}"), formularioSuperusuario, Cancelacion);

        dgaa.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        superusuario.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("  Visto bueno.  ", "Visto bueno.")]
    public async Task Aceptar_ConYSinComentarios_Devuelve204(string? comentarios, string? esperado)
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync();

        using var respuesta = await EjecutarAsync(escenario.Oferta.Dgaa, id, "aceptar", new { comentarios });
        var aceptada = await ObtenerAsync(escenario.Entidad, id);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        aceptada.GetProperty("estado").GetString().ShouldBe("ACEPTADA");
        aceptada.GetProperty("resueltaEn").GetString().ShouldNotBeNull().ShouldMatch(PatronInstanteUtc);
        aceptada.GetProperty("resueltaPorUsuarioId").ValueKind.ShouldBe(JsonValueKind.Number);
        aceptada.GetProperty("comentariosResolucion").GetString().ShouldBe(esperado);
        aceptada.GetProperty("canceladaEn").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Aceptar_ConComentariosDemasiadoLargos_Devuelve400EnComentarios()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync();

        using var respuesta = await EjecutarAsync(
            escenario.Oferta.Dgaa, id, "aceptar", new { comentarios = new string('x', 2001) });

        await VerificaErrorDeCampoAsync(respuesta, "comentarios", "SolicitudApertura.ComentariosDemasiadoLargos");
    }

    [Fact]
    public async Task Aceptar_ConExperienciaSinCupoMaximo_Devuelve409CuposIncompletos()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api, cupoMinimo: 10, cupoMaximo: null);
        var (id, _) = await escenario.CrearSolicitudAsync();

        using var respuesta = await EjecutarAsync(escenario.Oferta.Dgaa, id, "aceptar", new { comentarios = (string?)null });

        await VerificaConflictoAsync(respuesta, "SolicitudApertura.CuposIncompletos");
    }

    [Fact]
    public async Task Aceptar_DespuesDeReducirLosCuposDeLaExperiencia_Devuelve409CantidadFueraDeCupos()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync(cantidad: 20);
        await ModificarCuposAsync(escenario, cupoMinimo: 10, cupoMaximo: 15);

        using var respuesta = await EjecutarAsync(escenario.Oferta.Dgaa, id, "aceptar", new { comentarios = (string?)null });

        await VerificaConflictoAsync(respuesta, "SolicitudApertura.CantidadFueraDeCupos");
        (await ObtenerAsync(escenario.Entidad, id)).GetProperty("estado").GetString().ShouldBe("PENDIENTE");
    }

    [Fact]
    public async Task Aceptar_FueraDePendiente_Devuelve409NoPendiente()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var id = await InsertarTerminalAsync(escenario, "RECHAZADA");

        using var respuesta = await EjecutarAsync(escenario.Oferta.Dgaa, id, "aceptar", new { comentarios = (string?)null });

        await VerificaConflictoAsync(respuesta, "SolicitudApertura.NoPendiente");
    }

    [Fact]
    public async Task Aceptar_ConDgaaDeOtraArea_Devuelve404()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        using var otro = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync();

        using var respuesta = await EjecutarAsync(otro.Oferta.Dgaa, id, "aceptar", new { comentarios = (string?)null });

        await VerificaNoEncontradoAsync(respuesta);
        (await ObtenerAsync(escenario.Entidad, id)).GetProperty("estado").GetString().ShouldBe("PENDIENTE");
    }

    [Fact]
    public async Task AceptarYRechazar_SoloPermitenDgaa()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync();

        using var aceptarEntidad = await EjecutarAsync(escenario.Entidad, id, "aceptar", new { comentarios = (string?)null });
        using var aceptarSuperusuario = await EjecutarAsync(
            escenario.Superusuario, id, "aceptar", new { comentarios = (string?)null });
        using var rechazarEntidad = await EjecutarAsync(escenario.Entidad, id, "rechazar", new { comentarios = "No procede." });
        using var rechazarSuperusuario = await EjecutarAsync(
            escenario.Superusuario, id, "rechazar", new { comentarios = "No procede." });

        aceptarEntidad.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        aceptarSuperusuario.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        rechazarEntidad.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        rechazarSuperusuario.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Rechazar_ConComentarios_Devuelve204()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync();

        using var respuesta = await EjecutarAsync(
            escenario.Oferta.Dgaa, id, "rechazar", new { comentarios = "  No hay profesor disponible.  " });
        var rechazada = await ObtenerAsync(escenario.Entidad, id);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        rechazada.GetProperty("estado").GetString().ShouldBe("RECHAZADA");
        rechazada.GetProperty("resueltaEn").GetString().ShouldNotBeNull().ShouldMatch(PatronInstanteUtc);
        rechazada.GetProperty("comentariosResolucion").GetString().ShouldBe("No hay profesor disponible.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Rechazar_SinComentarios_Devuelve400EnComentarios(string? comentarios)
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync();

        using var respuesta = await EjecutarAsync(escenario.Oferta.Dgaa, id, "rechazar", new { comentarios });

        await VerificaErrorDeCampoAsync(respuesta, "comentarios", "SolicitudApertura.ComentariosVacios");
    }

    [Fact]
    public async Task Rechazar_FueraDePendiente_Devuelve409NoPendiente()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var id = await InsertarTerminalAsync(escenario, "ACEPTADA");

        using var respuesta = await EjecutarAsync(escenario.Oferta.Dgaa, id, "rechazar", new { comentarios = "No procede." });

        await VerificaConflictoAsync(respuesta, "SolicitudApertura.NoPendiente");
    }

    [Fact]
    public async Task Rechazar_ConDgaaDeOtraArea_Devuelve404()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        using var otro = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync();

        using var respuesta = await EjecutarAsync(otro.Oferta.Dgaa, id, "rechazar", new { comentarios = "No procede." });

        await VerificaNoEncontradoAsync(respuesta);
    }

    [Fact]
    public async Task Cancelar_ConMotivo_Devuelve204()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync();

        using var respuesta = await EjecutarAsync(escenario.Entidad, id, "cancelar", new { motivo = "  Ya no se necesita.  " });
        var cancelada = await ObtenerAsync(escenario.Entidad, id);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        cancelada.GetProperty("estado").GetString().ShouldBe("CANCELADA");
        cancelada.GetProperty("canceladaEn").GetString().ShouldNotBeNull().ShouldMatch(PatronInstanteUtc);
        cancelada.GetProperty("canceladaPorUsuarioId").ValueKind.ShouldBe(JsonValueKind.Number);
        cancelada.GetProperty("motivoCancelacion").GetString().ShouldBe("Ya no se necesita.");
        cancelada.GetProperty("resueltaEn").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Cancelar_SinMotivo_Devuelve400EnMotivo(string? motivo)
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync();

        using var respuesta = await EjecutarAsync(escenario.Entidad, id, "cancelar", new { motivo });

        await VerificaErrorDeCampoAsync(respuesta, "motivo", "SolicitudApertura.MotivoVacio");
    }

    [Fact]
    public async Task Cancelar_ConMotivoDeMasDeMilCaracteres_Devuelve400EnMotivo()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync();

        using var respuesta = await EjecutarAsync(escenario.Entidad, id, "cancelar", new { motivo = new string('x', 1001) });

        await VerificaErrorDeCampoAsync(respuesta, "motivo", "SolicitudApertura.MotivoDemasiadoLargo");
    }

    [Fact]
    public async Task Cancelar_FueraDePendiente_Devuelve409NoPendiente()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var id = await InsertarTerminalAsync(escenario, "RECHAZADA");

        using var respuesta = await EjecutarAsync(escenario.Entidad, id, "cancelar", new { motivo = "Ya no se necesita." });

        await VerificaConflictoAsync(respuesta, "SolicitudApertura.NoPendiente");
    }

    [Fact]
    public async Task Cancelar_ConSolicitudDeOtraEntidad_Devuelve404()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        using var otro = await EscenarioSolicitud.CrearAsync(_api);
        var (idAjeno, _) = await otro.CrearSolicitudAsync();

        using var respuesta = await EjecutarAsync(escenario.Entidad, idAjeno, "cancelar", new { motivo = "Ya no se necesita." });

        await VerificaNoEncontradoAsync(respuesta);
        (await ObtenerAsync(otro.Entidad, idAjeno)).GetProperty("estado").GetString().ShouldBe("PENDIENTE");
    }

    [Fact]
    public async Task Cancelar_SoloPermiteEntidadAcademica()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync();

        using var dgaa = await EjecutarAsync(escenario.Oferta.Dgaa, id, "cancelar", new { motivo = "Ya no se necesita." });
        using var superusuario = await EjecutarAsync(escenario.Superusuario, id, "cancelar", new { motivo = "Ya no se necesita." });

        dgaa.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        superusuario.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BajaDeExperiencia_DespuesDeAceptar_Devuelve409TieneReferencias()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync();
        using var aceptar = await EjecutarAsync(escenario.Oferta.Dgaa, id, "aceptar", new { comentarios = (string?)null });
        aceptar.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var baja = await escenario.Oferta.Dgaa.DeleteAsync(RutaExperiencia(escenario.ExperienciaId), Cancelacion);

        await VerificaConflictoAsync(baja, "ExperienciaEducativa.TieneReferencias");
    }

    [Theory]
    [InlineData("rechazar")]
    [InlineData("cancelar")]
    public async Task BajaDeExperiencia_DespuesDeRechazarOCancelar_Devuelve204(string accion)
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        var (id, _) = await escenario.CrearSolicitudAsync();
        using var transicion = accion == "rechazar"
            ? await EjecutarAsync(escenario.Oferta.Dgaa, id, accion, new { comentarios = "No procede." })
            : await EjecutarAsync(escenario.Entidad, id, accion, new { motivo = "Ya no se necesita." });
        transicion.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var baja = await escenario.Oferta.Dgaa.DeleteAsync(RutaExperiencia(escenario.ExperienciaId), Cancelacion);

        baja.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static Uri Uri(string sufijo = "") => new($"{Ruta}{sufijo}", UriKind.Relative);

    private Task<int> InsertarAsync(EscenarioSolicitud escenario, string estado, string seccion, DateTime creadaEn) =>
        DatosSolicitudesSql.InsertarAsync(
            sqlServer.CadenaConexion, escenario.ExperienciaId, escenario.PeriodoSiguienteId, estado, seccion, creadaEn);

    private static async Task<List<int>> ListarIdsAsync(HttpClient cliente, string query)
    {
        using var respuesta = await cliente.GetAsync(Uri(query), Cancelacion);
        var contenido = await respuesta.Content.ReadAsStringAsync(Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK, contenido);
        return (await EscenarioOferta.Leer(respuesta)).GetProperty("elementos").EnumerateArray()
            .Select(e => e.GetProperty("id").GetInt32()).ToList();
    }

    private static async Task VerificaListadoVacioAsync(HttpClient cliente, string query)
    {
        using var respuesta = await cliente.GetAsync(Uri(query), Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var pagina = await EscenarioOferta.Leer(respuesta);
        pagina.GetProperty("elementos").GetArrayLength().ShouldBe(0);
        pagina.GetProperty("total").GetInt32().ShouldBe(0);
    }

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

    private static Uri RutaExperiencia(int id) => new($"/api/v1/oferta-educativa/experiencias-educativas/{id}", UriKind.Relative);

    private static async Task<JsonElement> ObtenerAsync(HttpClient cliente, int id)
    {
        using var respuesta = await cliente.GetAsync(Uri($"/{id}"), Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await EscenarioOferta.Leer(respuesta);
    }

    private static Task<HttpResponseMessage> EjecutarAsync(HttpClient cliente, int id, string accion, object cuerpo) =>
        cliente.PostAsJsonAsync(Uri($"/{id}/{accion}"), cuerpo, Cancelacion);

    /// <summary>Una solicitud en estado terminal de la EE y el periodo del escenario, con una sección propia.</summary>
    private Task<int> InsertarTerminalAsync(EscenarioSolicitud escenario, string estado) =>
        DatosSolicitudesSql.InsertarAsync(
            sqlServer.CadenaConexion,
            escenario.ExperienciaId,
            escenario.PeriodoSiguienteId,
            estado,
            $"T{DatosUnicos.ClaveAlfanumerica()[..8]}");

    /// <summary>Cambia solo los cupos de la EE con los demás valores que ya tiene, como lo haría la DGAA.</summary>
    private static async Task ModificarCuposAsync(EscenarioSolicitud escenario, int cupoMinimo, int cupoMaximo)
    {
        using var lectura = await escenario.Oferta.Dgaa.GetAsync(RutaExperiencia(escenario.ExperienciaId), Cancelacion);
        lectura.StatusCode.ShouldBe(HttpStatusCode.OK);
        var ee = await EscenarioOferta.Leer(lectura);

        using var modificacion = await escenario.Oferta.Dgaa.PutAsJsonAsync(
            RutaExperiencia(escenario.ExperienciaId),
            new
            {
                nombre = ee.GetProperty("nombre").GetString(),
                horasTeoricas = ee.GetProperty("horasTeoricas").GetInt32(),
                horasPracticas = ee.GetProperty("horasPracticas").GetInt32(),
                creditos = ee.GetProperty("creditos").GetInt32(),
                cupoMinimo,
                cupoMaximo,
                perfilDocente = ee.GetProperty("perfilDocente").GetString(),
                areaFormacionId = ee.GetProperty("areaFormacion").GetProperty("id").GetInt32(),
            },
            Cancelacion);
        modificacion.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private async Task<(int Id, string Clave)> LeerOficioAsync(int solicitudId)
    {
        await using var conexion = new SqlConnection(sqlServer.CadenaConexion);
        await conexion.OpenAsync(Cancelacion);
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            SELECT archivo.id, archivo.clave_almacenamiento
            FROM academico.solicitud_apertura AS solicitud
            JOIN academico.archivo_solicitud_apertura AS archivo ON archivo.id = solicitud.oficio_respaldo_id
            WHERE solicitud.id = @id
            """;
        comando.Parameters.AddWithValue("@id", solicitudId);
        await using var lector = await comando.ExecuteReaderAsync(Cancelacion);
        (await lector.ReadAsync(Cancelacion)).ShouldBeTrue();
        return (lector.GetInt32(0), lector.GetString(1));
    }

    private async Task<bool> ExisteArchivoAsync(int archivoId)
    {
        await using var conexion = new SqlConnection(sqlServer.CadenaConexion);
        await conexion.OpenAsync(Cancelacion);
        await using var comando = conexion.CreateCommand();
        comando.CommandText = "SELECT COUNT(*) FROM academico.archivo_solicitud_apertura WHERE id = @id";
        comando.Parameters.AddWithValue("@id", archivoId);
        return (int)(await comando.ExecuteScalarAsync(Cancelacion))! > 0;
    }

    private static async Task VerificaConflictoAsync(HttpResponseMessage respuesta, string codigo)
    {
        var problema = await EscenarioOferta.Leer(respuesta);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problema.GetProperty("codigo").GetString().ShouldBe(codigo);
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
