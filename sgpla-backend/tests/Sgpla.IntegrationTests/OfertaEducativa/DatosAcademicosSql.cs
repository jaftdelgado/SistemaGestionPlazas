using Microsoft.Data.SqlClient;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests.OfertaEducativa;

/// <summary>
/// Inserta por SQL lo que todavía no tiene endpoint: planes de estudio (PR 3), y programaciones académicas, sus
/// sincronizaciones y sus horarios (los crea la sincronización con PLANEA). Respeta los CHECK del esquema; los datos
/// que no importan a la prueba son fijos.
/// </summary>
internal static class DatosAcademicosSql
{
    // Semilla: sistema educativo 1 (Escolarizada), nivel de formación 3 (LIC) y área de formación 1 (111).
    private const int SistemaEducativoId = 1;
    private const int NivelFormacionId = 3;
    private const int AreaFormacionId = 1;

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    /// <summary>Inserta un plan del programa con un código único válido, activo o dado de baja; devuelve su id.</summary>
    public static async Task<int> InsertarPlanAsync(
        string cadenaConexion, int programaEducativoId, bool dadoDeBaja = false)
    {
        await using var conexion = new SqlConnection(cadenaConexion);
        await conexion.OpenAsync(Cancelacion);
        await using var comando = new SqlCommand(
            """
            INSERT INTO academico.plan_estudios (codigo, programa_educativo_id, fecha_eliminacion)
            OUTPUT INSERTED.id
            VALUES (@codigo, @programaId, CASE WHEN @dadoDeBaja = 1 THEN SYSUTCDATETIME() ELSE NULL END);
            """,
            conexion);
        comando.Parameters.AddWithValue("@codigo", CodigoUnico());
        comando.Parameters.AddWithValue("@programaId", programaEducativoId);
        comando.Parameters.AddWithValue("@dadoDeBaja", dadoDeBaja);

        return (int)(await comando.ExecuteScalarAsync(Cancelacion))!;
    }

    /// <summary>
    /// Inserta una programación del periodo, activa o dada de baja, colgada de la entidad: crea por SQL un programa, un
    /// plan y una experiencia educativa nuevos en esa entidad. Devuelve el id de la programación.
    /// </summary>
    public static async Task<int> InsertarProgramacionAsync(
        string cadenaConexion, int periodoEscolarId, int entidadAcademicaId, bool dadaDeBaja = false)
    {
        await using var conexion = new SqlConnection(cadenaConexion);
        await conexion.OpenAsync(Cancelacion);
        await using var comando = new SqlCommand(
            """
            DECLARE @programaId int, @planId int, @experienciaId int;

            INSERT INTO academico.programa_educativo (nombre, entidad_academica_id, sistema_educativo_id, nivel_formacion_id)
            VALUES (@nombrePrograma, @entidadId, @sistemaId, @nivelId);
            SET @programaId = SCOPE_IDENTITY();

            INSERT INTO academico.plan_estudios (codigo, programa_educativo_id)
            VALUES (@codigoPlan, @programaId);
            SET @planId = SCOPE_IDENTITY();

            INSERT INTO academico.experiencia_educativa
                (nombre, materia_ee, curso_ee, horas_teoricas, horas_practicas, creditos, area_formacion_id, plan_estudios_id)
            VALUES (N'Experiencia educativa de prueba', @materia, '00001', 2, 2, 4, @areaFormacionId, @planId);
            SET @experienciaId = SCOPE_IDENTITY();

            INSERT INTO academico.programacion_academica (nrc, periodo_escolar_id, experiencia_educativa_id, fecha_eliminacion)
            OUTPUT INSERTED.id
            VALUES (@nrc, @periodoId, @experienciaId, CASE WHEN @dadaDeBaja = 1 THEN SYSUTCDATETIME() ELSE NULL END);
            """,
            conexion);
        comando.Parameters.AddWithValue("@nombrePrograma", DatosUnicos.Nombre("Programa de prueba"));
        comando.Parameters.AddWithValue("@entidadId", entidadAcademicaId);
        comando.Parameters.AddWithValue("@sistemaId", SistemaEducativoId);
        comando.Parameters.AddWithValue("@nivelId", NivelFormacionId);
        comando.Parameters.AddWithValue("@codigoPlan", CodigoUnico());
        comando.Parameters.AddWithValue("@materia", DatosUnicos.ClaveAlfanumerica());
        comando.Parameters.AddWithValue("@areaFormacionId", AreaFormacionId);
        comando.Parameters.AddWithValue("@nrc", Nrc());
        comando.Parameters.AddWithValue("@periodoId", periodoEscolarId);
        comando.Parameters.AddWithValue("@dadaDeBaja", dadaDeBaja);

        return (int)(await comando.ExecuteScalarAsync(Cancelacion))!;
    }

    /// <summary>
    /// Inserta solo la programación de una experiencia educativa que ya existe, activa o dada de baja. Con
    /// <paramref name="nrc"/> en <c>null</c> usa un NRC único aleatorio. Devuelve el id de la programación.
    /// </summary>
    public static async Task<int> InsertarProgramacionDeExperienciaAsync(
        string cadenaConexion, int periodoEscolarId, int experienciaEducativaId, bool dadaDeBaja = false, string? nrc = null)
    {
        await using var conexion = new SqlConnection(cadenaConexion);
        await conexion.OpenAsync(Cancelacion);
        await using var comando = new SqlCommand(
            """
            INSERT INTO academico.programacion_academica (nrc, periodo_escolar_id, experiencia_educativa_id, fecha_eliminacion)
            OUTPUT INSERTED.id
            VALUES (@nrc, @periodoId, @experienciaId, CASE WHEN @dadaDeBaja = 1 THEN SYSUTCDATETIME() ELSE NULL END);
            """,
            conexion);
        comando.Parameters.AddWithValue("@nrc", nrc ?? Nrc());
        comando.Parameters.AddWithValue("@periodoId", periodoEscolarId);
        comando.Parameters.AddWithValue("@experienciaId", experienciaEducativaId);
        comando.Parameters.AddWithValue("@dadaDeBaja", dadaDeBaja);

        return (int)(await comando.ExecuteScalarAsync(Cancelacion))!;
    }

    /// <summary>Inserta una sincronización EXITOSA del periodo, con fechas válidas; devuelve su id.</summary>
    public static async Task<int> InsertarSincronizacionAsync(string cadenaConexion, int periodoEscolarId)
    {
        await using var conexion = new SqlConnection(cadenaConexion);
        await conexion.OpenAsync(Cancelacion);
        await using var comando = new SqlCommand(
            """
            INSERT INTO integracion.sincronizacion_planea (periodo_escolar_id, estado, iniciada_en, finalizada_en)
            OUTPUT INSERTED.id
            VALUES (@periodoId, 'EXITOSA', SYSUTCDATETIME(), SYSUTCDATETIME());
            """,
            conexion);
        comando.Parameters.AddWithValue("@periodoId", periodoEscolarId);

        return (int)(await comando.ExecuteScalarAsync(Cancelacion))!;
    }

    /// <summary>Inserta una sesión de horario de la programación; devuelve su id.</summary>
    public static async Task<int> InsertarHorarioAsync(
        string cadenaConexion,
        int programacionAcademicaId,
        int sincronizacionPlaneaId,
        byte diaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        DateOnly fechaInicio,
        DateOnly fechaFin,
        string? edificio = null,
        string? aula = null)
    {
        await using var conexion = new SqlConnection(cadenaConexion);
        await conexion.OpenAsync(Cancelacion);
        await using var comando = new SqlCommand(
            """
            INSERT INTO academico.horario_programacion
                (programacion_academica_id, sincronizacion_planea_id, dia_semana, hora_inicio, hora_fin,
                 fecha_inicio, fecha_fin, edificio, aula)
            OUTPUT INSERTED.id
            VALUES (@programacionId, @sincronizacionId, @dia, @horaInicio, @horaFin, @fechaInicio, @fechaFin, @edificio, @aula);
            """,
            conexion);
        comando.Parameters.AddWithValue("@programacionId", programacionAcademicaId);
        comando.Parameters.AddWithValue("@sincronizacionId", sincronizacionPlaneaId);
        comando.Parameters.AddWithValue("@dia", diaSemana);
        comando.Parameters.AddWithValue("@horaInicio", horaInicio);
        comando.Parameters.AddWithValue("@horaFin", horaFin);
        comando.Parameters.AddWithValue("@fechaInicio", fechaInicio);
        comando.Parameters.AddWithValue("@fechaFin", fechaFin);
        comando.Parameters.AddWithValue("@edificio", (object?)edificio ?? DBNull.Value);
        comando.Parameters.AddWithValue("@aula", (object?)aula ?? DBNull.Value);

        return (int)(await comando.ExecuteScalarAsync(Cancelacion))!;
    }

    /// <summary>Código de plan válido (A-Z, 0-9 y guion) y único: <c>PLAN-</c> y 20 hexadecimales en mayúsculas.</summary>
    private static string CodigoUnico() => $"PLAN-{DatosUnicos.ClaveAlfanumerica()}";

    /// <summary>NRC válido (A-Z y 0-9) de 10 caracteres; único por periodo, y con 40 bits de azar lo es en toda la base.</summary>
    private static string Nrc() => DatosUnicos.ClaveAlfanumerica()[..10];
}
