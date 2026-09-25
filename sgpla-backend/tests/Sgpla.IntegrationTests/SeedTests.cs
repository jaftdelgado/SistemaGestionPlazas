using Microsoft.Data.SqlClient;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests;

public sealed class SeedTests(SqlServerFixture sqlServer)
{
    [Theory]
    [InlineData("usuarios.rol", 3)]
    [InlineData("academico.municipio", 212)]
    [InlineData("academico.sistema_educativo", 6)]
    [InlineData("academico.nivel_formacion", 6)]
    [InlineData("academico.area_formacion", 5)]
    [InlineData("academico.region", 5)]
    [InlineData("academico.campus", 24)]
    [InlineData("academico.grado_academico", 4)]
    // Omitidos a propósito: los registra el Superusuario desde la aplicación.
    [InlineData("academico.area_academica", 0)]
    [InlineData("academico.periodo_escolar", 0)]
    [InlineData("academico.entidad_academica", 0)]
    public async Task Seed_CargaLosCatalogosEsperados(string tabla, int filasEsperadas)
    {
        await using var conexion = new SqlConnection(sqlServer.CadenaConexion);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new SqlCommand($"SELECT COUNT(*) FROM {tabla}", conexion);

        var filas = (int)(await comando.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;

        filas.ShouldBe(filasEsperadas);
    }

    [Fact]
    public async Task Seed_CargaLosTresRolesFijos()
    {
        await using var conexion = new SqlConnection(sqlServer.CadenaConexion);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new SqlCommand("SELECT id, nombre FROM usuarios.rol ORDER BY id", conexion);

        var roles = new List<(byte Id, string Nombre)>();
        await using var lector = await comando.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await lector.ReadAsync(TestContext.Current.CancellationToken))
        {
            roles.Add((lector.GetByte(0), lector.GetString(1)));
        }

        roles.ShouldBe([(1, "Superusuario"), (2, "DGAA"), (3, "Entidad Académica")]);
    }

    [Fact]
    public async Task Seed_CargaLosGradosAcademicosFijos()
    {
        await using var conexion = new SqlConnection(sqlServer.CadenaConexion);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new SqlCommand("SELECT id, nombre FROM academico.grado_academico ORDER BY id", conexion);

        var grados = new List<(int Id, string Nombre)>();
        await using var lector = await comando.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await lector.ReadAsync(TestContext.Current.CancellationToken))
        {
            grados.Add((lector.GetInt32(0), lector.GetString(1)));
        }

        grados.ShouldBe([(1, "Licenciatura"), (2, "Especialidad"), (3, "Maestría"), (4, "Doctorado")]);
    }
}
