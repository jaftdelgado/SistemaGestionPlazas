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
    [InlineData("academico.area_formacion", 3)]
    [InlineData("academico.region", 5)]
    [InlineData("academico.campus", 24)]
    [InlineData("academico.grado_academico", 4)]
    [InlineData("plazas.tratamiento_academico", 5)]
    // Catálogos fijos cuyos valores aún no están definidos.
    [InlineData("academico.tipo_documento_expediente", 0)]
    [InlineData("plazas.modalidad_recepcion", 0)]
    [InlineData("plazas.tipo_plaza", 0)]
    [InlineData("plazas.tipo_contratacion", 0)]
    // Omitido a propósito: lo registra el Superusuario y las pruebas de integración crean periodos en la base
    // compartida, por lo que su conteo global no es estable.
    // academico.area_academica y academico.entidad_academica ya no son de conteo fijo: el módulo Institucional
    // agregó sus endpoints de creación, y las pruebas de integración insertan filas reales ahí (igual que
    // plazas.articulo, que por la misma razón tampoco se afirma en cero).
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

    [Fact]
    public async Task Seed_CargaLosTratamientosAcademicosFijos()
    {
        await using var conexion = new SqlConnection(sqlServer.CadenaConexion);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new SqlCommand(
            "SELECT id, nombre, grado_academico_id FROM plazas.tratamiento_academico ORDER BY id", conexion);

        var tratamientos = new List<(int Id, string Nombre, int GradoAcademicoId)>();
        await using var lector = await comando.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await lector.ReadAsync(TestContext.Current.CancellationToken))
        {
            tratamientos.Add((lector.GetInt32(0), lector.GetString(1), lector.GetInt32(2)));
        }

        tratamientos.ShouldBe([(1, "Lic", 1), (2, "Mtro", 3), (3, "Mtra", 3), (4, "Dr", 4), (5, "Dra", 4)]);
    }
}
