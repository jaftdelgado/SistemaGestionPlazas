using DbUp.Engine.Output;
using Npgsql;
using Sgpla.Database;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests;

public sealed class MigracionesTests(PostgreSqlFixture baseDatos)
{
    [Theory]
    [InlineData(DatabaseMigrator.NombreBaseline)]
    [InlineData(DatabaseMigrator.NombreSeed)]
    public async Task BaselineYSeed_QuedanRegistradosUnaSolaVezEnLaBitacora(string nombreScript)
    {
        var registros = await ContarAsync(
            baseDatos.CadenaConexion,
            $"SELECT COUNT(*) FROM public.schema_versions WHERE scriptname = '{nombreScript}'");

        registros.ShouldBe(1);
    }

    [Fact]
    public void Migrar_SobreBaseYaMigrada_NoEjecutaNingunScript()
    {
        var resultado = DatabaseMigrator.Migrar(baseDatos.CadenaConexion, log: new NoOpUpgradeLog());

        resultado.Successful.ShouldBeTrue();
        resultado.Scripts.ShouldBeEmpty();
    }

    [Fact]
    public async Task Migrar_SobreBaseConTablasSinBaseline_FallaSinModificarla()
    {
        var servidor = new NpgsqlConnectionStringBuilder(baseDatos.CadenaConexion) { Database = "postgres" };
        await EjecutarAsync(servidor.ConnectionString, "CREATE DATABASE \"sgpla-sin-baseline\";");
        var cadenaConexion = new NpgsqlConnectionStringBuilder(servidor.ConnectionString)
        {
            Database = "sgpla-sin-baseline",
        }.ConnectionString;
        await EjecutarAsync(cadenaConexion, "CREATE TABLE public.existente (id int NOT NULL);");

        var resultado = DatabaseMigrator.Migrar(cadenaConexion, log: new NoOpUpgradeLog());

        resultado.Successful.ShouldBeFalse();
        resultado.Error.ShouldBeOfType<InvalidOperationException>();
        (await ContarAsync(
            cadenaConexion,
            "SELECT COUNT(*) FROM pg_tables WHERE schemaname NOT IN ('pg_catalog', 'information_schema')")).ShouldBe(1);
    }

    private static async Task<int> ContarAsync(string cadenaConexion, string consulta)
    {
        await using var conexion = new NpgsqlConnection(cadenaConexion);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new NpgsqlCommand(consulta, conexion);
        return (int)(long)(await comando.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }

    private static async Task EjecutarAsync(string cadenaConexion, string sentencia)
    {
        await using var conexion = new NpgsqlConnection(cadenaConexion);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new NpgsqlCommand(sentencia, conexion);
        await comando.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
