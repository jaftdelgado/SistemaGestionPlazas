using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Sgpla.IntegrationTests.Infraestructura;
using Sgpla.IntegrationTests.OfertaEducativa;

namespace Sgpla.IntegrationTests.SolicitudesApertura;

/// <summary>Oferta, periodo, usuario de Entidad Académica y EE que pertenecen a una prueba.</summary>
internal sealed class EscenarioSolicitud : IDisposable
{
    private EscenarioSolicitud()
    {
    }

    public required SgplaApiFactory Api { get; init; }

    public required EscenarioOferta Oferta { get; init; }

    public required HttpClient Superusuario { get; init; }

    public required HttpClient Entidad { get; init; }

    public required int PlanId { get; init; }

    public required string PlanCodigo { get; init; }

    public required int ExperienciaId { get; init; }

    public required int PeriodoActualId { get; init; }

    public required int PeriodoSiguienteId { get; init; }

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    public static async Task<EscenarioSolicitud> CrearAsync(
        SgplaApiFactory api,
        int? cupoMinimo = 10,
        int? cupoMaximo = 40)
    {
        var superusuario = await api.CrearClienteSuperusuarioAsync();
        var oferta = await EscenarioOferta.CrearAsync(api, superusuario);
        var entidad = await api.CrearClienteEntidadAcademicaAsync(oferta.EntidadId);
        var planCodigo = EscenarioOferta.CodigoDePlan();
        var planId = await EscenarioOferta.CrearPlanAsync(
            oferta.Dgaa,
            oferta.ProgramaId,
            planCodigo,
            [EscenarioOferta.Experiencia(materia: "ENSO", cupoMinimo: cupoMinimo, cupoMaximo: cupoMaximo)]);

        using var experiencias = await oferta.Dgaa.GetAsync(
            new Uri($"/api/v1/oferta-educativa/planes-estudio/{planId}/experiencias-educativas", UriKind.Relative),
            Cancelacion);
        experiencias.StatusCode.ShouldBe(HttpStatusCode.OK);
        var ee = await EscenarioOferta.Leer(experiencias);
        var experienciaId = ee.EnumerateArray().ShouldHaveSingleItem().GetProperty("id").GetInt32();

        using var periodos = await superusuario.GetAsync(
            new Uri("/api/v1/solicitudes-apertura/periodos", UriKind.Relative), Cancelacion);
        periodos.StatusCode.ShouldBe(HttpStatusCode.OK);
        var pareja = await EscenarioOferta.Leer(periodos);

        return new EscenarioSolicitud
        {
            Api = api,
            Oferta = oferta,
            Superusuario = superusuario,
            Entidad = entidad,
            PlanId = planId,
            PlanCodigo = planCodigo,
            ExperienciaId = experienciaId,
            PeriodoActualId = pareja.GetProperty("actual").GetProperty("id").GetInt32(),
            PeriodoSiguienteId = pareja.GetProperty("siguiente").GetProperty("id").GetInt32(),
        };
    }

    public MultipartFormDataContent CrearFormulario(
        int? experienciaId = null,
        int? periodoId = null,
        string? seccion = "A1",
        int? cantidad = 20,
        string? justificacion = "Solicitud de apertura.",
        bool agregarOficio = true,
        byte[]? bytes = null,
        string nombreArchivo = "oficio.pdf",
        string tipoContenido = "application/pdf")
    {
        var formulario = new MultipartFormDataContent();
        Agregar(formulario, "experienciaEducativaId", experienciaId ?? ExperienciaId);
        Agregar(formulario, "periodoEscolarId", periodoId ?? PeriodoSiguienteId);
        if (seccion is not null)
        {
            formulario.Add(new StringContent(seccion, Encoding.UTF8), "seccion");
        }

        if (cantidad is not null)
        {
            Agregar(formulario, "cantidadEstudiantes", cantidad.Value);
        }

        if (justificacion is not null)
        {
            formulario.Add(new StringContent(justificacion, Encoding.UTF8), "justificacion");
        }

        if (agregarOficio)
        {
            var archivo = new ByteArrayContent(bytes ?? Pdf);
            archivo.Headers.ContentType = MediaTypeHeaderValue.Parse(tipoContenido);
            formulario.Add(archivo, "oficio", nombreArchivo);
        }

        return formulario;
    }

    public async Task<(int Id, JsonElement Respuesta)> CrearSolicitudAsync(
        HttpClient? cliente = null,
        string? seccion = null,
        int? cantidad = null,
        int? experienciaId = null,
        int? periodoId = null)
    {
        using var formulario = CrearFormulario(
            experienciaId,
            periodoId,
            seccion ?? $"S{DatosUnicos.ClaveAlfanumerica()[..8]}",
            cantidad ?? 20);
        using var respuesta = await (cliente ?? Entidad).PostAsync(
            new Uri("/api/v1/solicitudes-apertura/solicitudes", UriKind.Relative), formulario, Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var cuerpo = await EscenarioOferta.Leer(respuesta);
        return (cuerpo.GetProperty("id").GetInt32(), cuerpo);
    }

    public void Dispose()
    {
        Entidad.Dispose();
        Superusuario.Dispose();
        Oferta.Dispose();
    }

    private static void Agregar(MultipartFormDataContent formulario, string nombre, int valor) =>
        formulario.Add(new StringContent(valor.ToString(CultureInfo.InvariantCulture), Encoding.UTF8), nombre);

    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7\ncontenido de prueba");
}
