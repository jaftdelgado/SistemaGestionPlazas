using System.Text;
using DbUp;
using DbUp.Engine;
using DbUp.Engine.Output;
using Microsoft.Data.SqlClient;

namespace Sgpla.Database;

/// <summary>
/// Construye y actualiza la base de datos en dos pasos:
/// <list type="number">
///   <item><c>Baseline/baseline.sql</c> y <c>Baseline/seed.sql</c> crean la base completa con sus datos
///   iniciales, en una sola transacción y solo si la base está vacía.</item>
///   <item><c>Scripts/####__descripcion.sql</c> aplica, en orden, los cambios posteriores al baseline.</item>
/// </list>
/// Todo se registra en <c>dbo.schema_versions</c>; el baseline y el seed con los nombres
/// <see cref="NombreBaseline"/> y <see cref="NombreSeed"/>.
/// Lo usan la CLI de este proyecto y el fixture de las pruebas de integración.
/// </summary>
public static class DatabaseMigrator
{
    public const string EsquemaBitacora = "dbo";
    public const string TablaBitacora = "schema_versions";
    public const string NombreBaseline = "baseline";
    public const string NombreSeed = "baseline-seed";

    private const string RecursoBaseline = "Sgpla.Database.Baseline.baseline.sql";
    private const string RecursoSeed = "Sgpla.Database.Baseline.seed.sql";
    private const string PrefijoRecursosMigraciones = "Sgpla.Database.Scripts.";

    /// <param name="cadenaConexion">Cadena de conexión a SQL Server.</param>
    /// <param name="crearBaseSiNoExiste">Solo para desarrollo y pruebas: crea la base de datos si no existe.</param>
    /// <param name="log">Destino de la bitácora de ejecución; por defecto, la consola.</param>
    public static DatabaseUpgradeResult Migrar(string cadenaConexion, bool crearBaseSiNoExiste = false, IUpgradeLog? log = null)
    {
        log ??= new ConsoleUpgradeLog();

        if (crearBaseSiNoExiste)
        {
            EnsureDatabase.For.SqlDatabase(cadenaConexion, log);
        }

        var baseline = new SqlScript(NombreBaseline, LeerRecurso(RecursoBaseline));

        EstadoBaseline estado;
        try
        {
            estado = ConsultarEstadoBaseline(cadenaConexion);
        }
        catch (SqlException ex)
        {
            return new DatabaseUpgradeResult([], false, ex, baseline);
        }

        if (estado == EstadoBaseline.BaseConDatosSinBaseline)
        {
            // Salvaguarda: nunca se ejecuta el baseline sobre una base que ya tiene tablas.
            var error = new InvalidOperationException(
                $"La base de datos ya contiene tablas pero no tiene registrado el '{NombreBaseline}' en " +
                $"{EsquemaBitacora}.{TablaBitacora}; no se aplicará el baseline sobre ella.");
            return new DatabaseUpgradeResult([], false, error, baseline);
        }

        var ejecutados = new List<SqlScript>();

        if (estado == EstadoBaseline.BaseVacia)
        {
            // DbUp ordena por nombre: 'baseline' se ejecuta antes que 'baseline-seed'. Una sola transacción
            // evita que un fallo del seed deje una base con estructura pero sin datos iniciales.
            var seed = new SqlScript(NombreSeed, LeerRecurso(RecursoSeed));
            var resultadoBaseline = CrearMotor(cadenaConexion, log)
                .WithScripts(baseline, seed)
                .WithTransaction()
                .Build()
                .PerformUpgrade();

            if (!resultadoBaseline.Successful)
            {
                return resultadoBaseline;
            }

            ejecutados.AddRange(resultadoBaseline.Scripts);
        }

        var resultadoMigraciones = CrearMotor(cadenaConexion, log)
            .WithScriptsEmbeddedInAssembly(
                typeof(DatabaseMigrator).Assembly,
                nombre => nombre.StartsWith(PrefijoRecursosMigraciones, StringComparison.Ordinal)
                    && nombre.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .WithTransactionPerScript()
            .Build()
            .PerformUpgrade();

        ejecutados.AddRange(resultadoMigraciones.Scripts);

        return new DatabaseUpgradeResult(
            ejecutados,
            resultadoMigraciones.Successful,
            resultadoMigraciones.Error,
            resultadoMigraciones.ErrorScript);
    }

    private static DbUp.Builder.UpgradeEngineBuilder CrearMotor(string cadenaConexion, IUpgradeLog log) =>
        DeployChanges.To
            .SqlDatabase(cadenaConexion)
            .JournalToSqlTable(EsquemaBitacora, TablaBitacora)
            .LogTo(log);

    private static EstadoBaseline ConsultarEstadoBaseline(string cadenaConexion)
    {
        // La bitácora puede no existir todavía. T-SQL no garantiza cortocircuito en AND, por eso
        // la consulta a la bitácora va en un IF anidado; la resolución diferida de nombres permite
        // compilar el lote aunque la tabla no exista.
        const string consulta =
            $"""
            IF OBJECT_ID(N'{EsquemaBitacora}.{TablaBitacora}', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (SELECT 1 FROM {EsquemaBitacora}.{TablaBitacora} WHERE ScriptName = N'{NombreBaseline}')
                BEGIN
                    SELECT 1; -- EstadoBaseline.Aplicado
                    RETURN;
                END
            END

            IF EXISTS (
                SELECT 1 FROM sys.tables
                WHERE NOT (schema_id = SCHEMA_ID(N'{EsquemaBitacora}') AND name = N'{TablaBitacora}'))
                SELECT 2; -- EstadoBaseline.BaseConDatosSinBaseline
            ELSE
                SELECT 0; -- EstadoBaseline.BaseVacia
            """;

        using var conexion = new SqlConnection(cadenaConexion);
        conexion.Open();
        using var comando = new SqlCommand(consulta, conexion);
        return (EstadoBaseline)(int)comando.ExecuteScalar()!;
    }

    private static string LeerRecurso(string nombre)
    {
        using var flujo = typeof(DatabaseMigrator).Assembly.GetManifestResourceStream(nombre)
            ?? throw new InvalidOperationException($"No se encontró el recurso embebido '{nombre}'.");
        using var lector = new StreamReader(flujo, Encoding.UTF8);
        return lector.ReadToEnd();
    }

    private enum EstadoBaseline
    {
        BaseVacia = 0,
        Aplicado = 1,
        BaseConDatosSinBaseline = 2,
    }
}
