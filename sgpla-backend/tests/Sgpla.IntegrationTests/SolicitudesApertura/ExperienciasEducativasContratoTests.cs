using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Sgpla.IntegrationTests.Infraestructura;
using Sgpla.IntegrationTests.OfertaEducativa;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;

namespace Sgpla.IntegrationTests.SolicitudesApertura;

public sealed class ExperienciasEducativasContratoTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private readonly SgplaApiFactory _api = new(sqlServer);

    [Fact]
    public async Task ObtenerAsync_DevuelveCadenaCuposVigenciaYOmiteInexistentes()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        await using var alcance = _api.Services.CreateAsyncScope();
        var contrato = alcance.ServiceProvider.GetRequiredService<IExperienciasEducativas>();

        var vacio = await contrato.ObtenerAsync([], TestContext.Current.CancellationToken);
        var resumenes = await contrato.ObtenerAsync(
            [escenario.ExperienciaId, int.MaxValue], TestContext.Current.CancellationToken);

        vacio.ShouldBeEmpty();
        resumenes.Count.ShouldBe(1);
        var resumen = resumenes[escenario.ExperienciaId];
        resumen.Vigente.ShouldBeTrue();
        resumen.CupoMinimo.ShouldBe(10);
        resumen.CupoMaximo.ShouldBe(40);
        resumen.PlanEstudiosCodigo.ShouldBe(escenario.PlanCodigo);
        resumen.ProgramaEducativoId.ShouldBe(escenario.Oferta.ProgramaId);
        resumen.EntidadAcademicaId.ShouldBe(escenario.Oferta.EntidadId);
        resumen.AreaAcademicaId.ShouldBe(escenario.Oferta.AreaId);
    }

    [Fact]
    public async Task ObtenerAsync_IncluyeLaExperienciaConPlanDadoDeBajaYLaMarcaNoVigente()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        await EjecutarSqlAsync(
            "UPDATE academico.plan_estudios SET fecha_eliminacion = '2026-10-01T15:04:05' WHERE id = @id",
            escenario.PlanId);
        await using var alcance = _api.Services.CreateAsyncScope();
        var contrato = alcance.ServiceProvider.GetRequiredService<IExperienciasEducativas>();

        var resumenes = await contrato.ObtenerAsync([escenario.ExperienciaId], TestContext.Current.CancellationToken);

        resumenes.ContainsKey(escenario.ExperienciaId).ShouldBeTrue();
        resumenes[escenario.ExperienciaId].Vigente.ShouldBeFalse();
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private async Task EjecutarSqlAsync(string sentencia, int id)
    {
        await using var conexion = new SqlConnection(sqlServer.CadenaConexion);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new SqlCommand(sentencia, conexion);
        comando.Parameters.AddWithValue("@id", id);
        await comando.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
