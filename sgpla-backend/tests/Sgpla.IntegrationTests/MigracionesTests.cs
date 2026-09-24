using DbUp.Engine.Output;
using Microsoft.Data.SqlClient;
using Sgpla.Database;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests;

public sealed class MigracionesTests(SqlServerFixture sqlServer)
{
    [Fact]
    public async Task Baseline_QuedaRegistradoUnaSolaVezEnLaBitacora()
    {
        var registros = await ContarAsync(
            sqlServer.CadenaConexion,
            $"SELECT COUNT(*) FROM dbo.schema_versions WHERE ScriptName = N'{DatabaseMigrator.NombreBaseline}'");

        registros.ShouldBe(1);
    }

    [Fact]
    public void Migrar_SobreBaseYaMigrada_NoEjecutaNingunScript()
    {
        var resultado = DatabaseMigrator.Migrar(sqlServer.CadenaConexion, log: new NoOpUpgradeLog());

        resultado.Successful.ShouldBeTrue();
        resultado.Scripts.ShouldBeEmpty();
    }

    [Fact]
    public async Task Migrar_SobreBaseConTablasSinBaseline_FallaSinModificarla()
    {
        var servidor = new SqlConnectionStringBuilder(sqlServer.CadenaConexion) { InitialCatalog = "master" };
        await EjecutarAsync(servidor.ConnectionString, "CREATE DATABASE [sgpla-sin-baseline];");
        var cadenaConexion = new SqlConnectionStringBuilder(servidor.ConnectionString)
        {
            InitialCatalog = "sgpla-sin-baseline",
        }.ConnectionString;
        await EjecutarAsync(cadenaConexion, "CREATE TABLE dbo.existente (id int NOT NULL);");

        var resultado = DatabaseMigrator.Migrar(cadenaConexion, log: new NoOpUpgradeLog());

        resultado.Successful.ShouldBeFalse();
        resultado.Error.ShouldBeOfType<InvalidOperationException>();
        (await ContarAsync(cadenaConexion, "SELECT COUNT(*) FROM sys.tables")).ShouldBe(1);
    }

    private static async Task<int> ContarAsync(string cadenaConexion, string consulta)
    {
        await using var conexion = new SqlConnection(cadenaConexion);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new SqlCommand(consulta, conexion);
        return (int)(await comando.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }

    private static async Task EjecutarAsync(string cadenaConexion, string sentencia)
    {
        await using var conexion = new SqlConnection(cadenaConexion);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new SqlCommand(sentencia, conexion);
        await comando.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
