using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests.OfertaEducativa;

/// <summary>
/// Datos propios de una prueba de planes o experiencias educativas: un área, una entidad, un programa y el cliente de su
/// DGAA, todo creado por la API con valores únicos. Cada prueba crea el suyo, así que no depende de los demás.
/// </summary>
internal sealed class EscenarioOferta : IDisposable
{
    // Semilla: sistema educativo 1 (Escolarizada), nivel de formación 3 (LIC) y área de formación 1 (111).
    private const int SistemaId = 1;
    private const int NivelId = 3;

    private static readonly Uri RutaAreas = new("/api/v1/institucional/areas-academicas", UriKind.Relative);
    private static readonly Uri RutaEntidades = new("/api/v1/institucional/entidades-academicas", UriKind.Relative);
    private static readonly Uri RutaProgramas = new("/api/v1/oferta-educativa/programas-educativos", UriKind.Relative);
    private static readonly Uri RutaPeriodos = new("/api/v1/oferta-educativa/periodos-escolares", UriKind.Relative);

    private EscenarioOferta()
    {
    }

    public required int AreaId { get; init; }

    public required string AreaNombre { get; init; }

    public required int EntidadId { get; init; }

    public required string EntidadClave { get; init; }

    public required string EntidadNombre { get; init; }

    public required int ProgramaId { get; init; }

    public required string ProgramaNombre { get; init; }

    /// <summary>Cliente del DGAA del área del escenario.</summary>
    public required HttpClient Dgaa { get; init; }

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    /// <summary>Crea un área, una entidad de esa área, su DGAA y un programa en la entidad.</summary>
    public static async Task<EscenarioOferta> CrearAsync(SgplaApiFactory api, HttpClient superusuario)
    {
        var (areaId, areaNombre) = await CrearAreaAsync(superusuario);
        var (entidadId, entidadClave, entidadNombre) = await CrearEntidadAsync(superusuario, areaId);
        var dgaa = await api.CrearClienteDgaaAsync(areaId);
        var (programaId, programaNombre) = await CrearProgramaAsync(dgaa, entidadId);

        return new EscenarioOferta
        {
            AreaId = areaId,
            AreaNombre = areaNombre,
            EntidadId = entidadId,
            EntidadClave = entidadClave,
            EntidadNombre = entidadNombre,
            ProgramaId = programaId,
            ProgramaNombre = programaNombre,
            Dgaa = dgaa,
        };
    }

    /// <summary>Crea un programa nuevo, con nombre único, en una entidad del área del DGAA.</summary>
    public static async Task<(int Id, string Nombre)> CrearProgramaAsync(HttpClient dgaa, int entidadAcademicaId)
    {
        var nombre = DatosUnicos.Nombre("Programa");
        using var respuesta = await dgaa.PostAsJsonAsync(
            RutaProgramas,
            new { nombre, entidadAcademicaId, sistemaEducativoId = SistemaId, nivelFormacionId = NivelId },
            Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return ((await Leer(respuesta)).GetProperty("id").GetInt32(), nombre);
    }

    public static async Task<int> CrearPeriodoAsync(HttpClient superusuario)
    {
        using var respuesta = await superusuario.PostAsJsonAsync(
            RutaPeriodos,
            new { clave = DatosUnicos.ClavePeriodo(), fechaInicio = "2026-08-10", fechaFin = "2027-01-22" },
            Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await Leer(respuesta)).GetProperty("id").GetInt32();
    }

    /// <summary>Importa un plan y espera 201; devuelve el id.</summary>
    public static async Task<int> CrearPlanAsync(
        HttpClient dgaa, int programaEducativoId, string codigo, IEnumerable<object> experiencias)
    {
        using var respuesta = await dgaa.PostAsJsonAsync(
            new Uri("/api/v1/oferta-educativa/planes-estudio", UriKind.Relative),
            new { programaEducativoId, codigo, experienciasEducativas = experiencias },
            Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await Leer(respuesta)).GetProperty("id").GetInt32();
    }

    /// <summary>Un código de plan único y válido.</summary>
    public static string CodigoDePlan() => $"PLAN-{DatosUnicos.ClaveAlfanumerica()}";

    /// <summary>Cuerpo de una EE válida; las claves de materia y curso son únicas salvo que se indiquen.</summary>
    public static object Experiencia(
        string? materia = null,
        string curso = "00001",
        string? nombre = null,
        int horasTeoricas = 2,
        int horasPracticas = 2,
        int creditos = 6,
        int? cupoMinimo = null,
        int? cupoMaximo = null,
        string? perfilDocente = null,
        int areaFormacionId = 1) => new
        {
            nombre = nombre ?? DatosUnicos.Nombre("Experiencia"),
            materia = materia ?? DatosUnicos.ClaveAlfanumerica(),
            curso,
            horasTeoricas,
            horasPracticas,
            creditos,
            cupoMinimo,
            cupoMaximo,
            perfilDocente,
            areaFormacionId,
        };

    public static async Task<JsonElement> Leer(HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>(Cancelacion);

    public void Dispose() => Dgaa.Dispose();

    private static async Task<(int Id, string Nombre)> CrearAreaAsync(HttpClient superusuario)
    {
        var nombre = DatosUnicos.Nombre("Área");
        using var respuesta = await superusuario.PostAsJsonAsync(
            RutaAreas,
            new { clave = DatosUnicos.ClaveEntera(), nombre, telefono = "2288421700", extension = (string?)null },
            Cancelacion);
        respuesta.StatusCode.ShouldBe(HttpStatusCode.Created);

        return ((await Leer(respuesta)).GetProperty("id").GetInt32(), nombre);
    }

    private static async Task<(int Id, string Clave, string Nombre)> CrearEntidadAsync(HttpClient superusuario, int areaAcademicaId)
    {
        var nombre = DatosUnicos.Nombre("Facultad");
        using var respuesta = await superusuario.PostAsJsonAsync(
            RutaEntidades,
            new
            {
                clave = DatosUnicos.ClaveAlfanumerica(),
                nombre,
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

        return (creada.GetProperty("id").GetInt32(), creada.GetProperty("clave").GetString()!, nombre);
    }
}
