using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sgpla.IntegrationTests.Infraestructura;
using Sgpla.IntegrationTests.SolicitudesApertura;
using Sgpla.Modules.OfertaEducativa.Application.Contracts;

namespace Sgpla.IntegrationTests.OfertaEducativa;

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

    [Fact]
    public async Task ObtenerAsync_IncluyeLaExperienciaDadaDeBajaYLaMarcaNoVigente()
    {
        using var escenario = await EscenarioSolicitud.CrearAsync(_api);
        using var baja = await escenario.Oferta.Dgaa.DeleteAsync(
            new Uri($"/api/v1/oferta-educativa/experiencias-educativas/{escenario.ExperienciaId}", UriKind.Relative),
            TestContext.Current.CancellationToken);
        baja.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await using var alcance = _api.Services.CreateAsyncScope();
        var contrato = alcance.ServiceProvider.GetRequiredService<IExperienciasEducativas>();

        var resumenes = await contrato.ObtenerAsync([escenario.ExperienciaId], TestContext.Current.CancellationToken);

        resumenes.ContainsKey(escenario.ExperienciaId).ShouldBeTrue();
        resumenes[escenario.ExperienciaId].Vigente.ShouldBeFalse();
    }

    [Fact]
    public async Task ConsultarIdsVisiblesAsync_RespetaElAmbitoDeCadaRol()
    {
        using var escenario1 = await EscenarioSolicitud.CrearAsync(_api);
        using var escenario2 = await EscenarioSolicitud.CrearAsync(_api);
        var ee1 = escenario1.ExperienciaId;
        var ee2 = escenario2.ExperienciaId;
        var entidad1 = escenario1.Oferta.EntidadId;
        var entidad2 = escenario2.Oferta.EntidadId;
        var area1 = escenario1.Oferta.AreaId;

        (await IdsVisiblesAsync(Claims(3, entidadAcademicaId: entidad1), null, ee1, ee2)).ShouldBe([ee1], ignoreOrder: true);
        (await IdsVisiblesAsync(Claims(2, areaAcademicaId: area1), null, ee1, ee2)).ShouldBe([ee1], ignoreOrder: true);
        (await IdsVisiblesAsync(Claims(1), null, ee1, ee2)).ShouldBe([ee1, ee2], ignoreOrder: true);
        (await IdsVisiblesAsync(Claims(3, entidadAcademicaId: entidad1), entidad2, ee1, ee2)).ShouldBeEmpty();
        (await IdsVisiblesAsync(Claims(1), entidad2, ee1, ee2)).ShouldBe([ee2], ignoreOrder: true);
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static List<Claim> Claims(int rol, int? areaAcademicaId = null, int? entidadAcademicaId = null)
    {
        var claims = new List<Claim> { new("sub", "1"), new("rol", rol.ToString(System.Globalization.CultureInfo.InvariantCulture)) };
        if (areaAcademicaId is { } area)
        {
            claims.Add(new Claim("area_academica_id", area.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        if (entidadAcademicaId is { } entidad)
        {
            claims.Add(new Claim("entidad_academica_id", entidad.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        return claims;
    }

    private async Task<List<int>> IdsVisiblesAsync(List<Claim> claims, int? entidadAcademicaId, int ee1, int ee2)
    {
        await using var alcance = _api.Services.CreateAsyncScope();
        var accesor = alcance.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accesor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Prueba")),
        };
        try
        {
            var contrato = alcance.ServiceProvider.GetRequiredService<IExperienciasEducativas>();
            var visibles = await contrato.ConsultarIdsVisiblesAsync(entidadAcademicaId, TestContext.Current.CancellationToken);
            return await visibles.Where(id => id == ee1 || id == ee2).ToListAsync(TestContext.Current.CancellationToken);
        }
        finally
        {
            accesor.HttpContext = null;
        }
    }

    private async Task EjecutarSqlAsync(string sentencia, int id)
    {
        await using var conexion = new SqlConnection(sqlServer.CadenaConexion);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new SqlCommand(sentencia, conexion);
        comando.Parameters.AddWithValue("@id", id);
        await comando.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
