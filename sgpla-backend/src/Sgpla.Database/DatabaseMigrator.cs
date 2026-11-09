using System.Text;
using DbUp;
using DbUp.Engine;
using DbUp.Engine.Output;
using Npgsql;

namespace Sgpla.Database;

/// <summary>
/// Construye y actualiza la base de datos en dos pasos:
/// <list type="number">
///   <item><c>Baseline/baseline.sql</c> y <c>Baseline/seed.sql</c> crean la base completa con sus datos
///   iniciales, en una sola transacción y solo si la base está vacía.</item>
///   <item><c>Scripts/####__descripcion.sql</c> aplica, en orden, los cambios posteriores al baseline.</item>
/// </list>
/// Todo se registra en <c>public.schema_versions</c>; el baseline y el seed con los nombres
/// <see cref="NombreBaseline"/> y <see cref="NombreSeed"/>.
/// Lo usan la CLI de este proyecto y el fixture de las pruebas de integración.
/// </summary>
public static class DatabaseMigrator
{
    public const string EsquemaBitacora = "public";
    public const string TablaBitacora = "schema_versions";
    public const string NombreBaseline = "baseline";
    public const string NombreSeed = "baseline-seed";

    private const string RecursoBaseline = "Sgpla.Database.Baseline.baseline.sql";
    private const string RecursoSeed = "Sgpla.Database.Baseline.seed.sql";
    private const string PrefijoRecursosMigraciones = "Sgpla.Database.Scripts.";

    /// <param name="cadenaConexion">Cadena de conexión a PostgreSQL.</param>
    /// <param name="crearBaseSiNoExiste">Solo para desarrollo y pruebas: crea la base de datos si no existe.</param>
    /// <param name="log">Destino de la bitácora de ejecución; por defecto, la consola.</param>
    public static DatabaseUpgradeResult Migrar(string cadenaConexion, bool crearBaseSiNoExiste = false, IUpgradeLog? log = null)
    {
        log ??= new ConsoleUpgradeLog();

        if (crearBaseSiNoExiste)
        {
            EnsureDatabase.For.PostgresqlDatabase(cadenaConexion, log);
        }

        var baseline = new SqlScript(NombreBaseline, LeerRecurso(RecursoBaseline));

        EstadoBaseline estado;
        try
        {
            estado = ConsultarEstadoBaseline(cadenaConexion);
        }
        catch (NpgsqlException ex)
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
            .PostgresqlDatabase(cadenaConexion)
            .JournalToPostgresqlTable(EsquemaBitacora, TablaBitacora)
            .LogTo(log);

    private static EstadoBaseline ConsultarEstadoBaseline(string cadenaConexion)
    {
        using var conexion = new NpgsqlConnection(cadenaConexion);
        conexion.Open();

        // La bitácora puede no existir todavía: PostgreSQL resuelve los nombres al analizar la sentencia,
        // así que se comprueba su existencia antes de consultarla.
        const string consultaBitacora = $"SELECT to_regclass('{EsquemaBitacora}.{TablaBitacora}') IS NOT NULL";
        if ((bool)Escalar(conexion, consultaBitacora))
        {
            const string consultaBaseline =
                $"SELECT EXISTS (SELECT 1 FROM {EsquemaBitacora}.{TablaBitacora} WHERE scriptname = '{NombreBaseline}')";
            if ((bool)Escalar(conexion, consultaBaseline))
            {
                return EstadoBaseline.Aplicado;
            }
        }

        const string consultaTablas =
            $"""
            SELECT EXISTS (
                SELECT 1 FROM pg_tables
                WHERE schemaname NOT IN ('pg_catalog', 'information_schema')
                  AND NOT (schemaname = '{EsquemaBitacora}' AND tablename = '{TablaBitacora}'))
            """;
        return (bool)Escalar(conexion, consultaTablas)
            ? EstadoBaseline.BaseConDatosSinBaseline
            : EstadoBaseline.BaseVacia;
    }

    private static object Escalar(NpgsqlConnection conexion, string consulta)
    {
        using var comando = new NpgsqlCommand(consulta, conexion);
        return comando.ExecuteScalar()!;
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
