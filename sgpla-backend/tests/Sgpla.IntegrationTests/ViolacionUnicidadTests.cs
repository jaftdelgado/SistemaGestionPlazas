using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Sgpla.BuildingBlocks.Infrastructure.Http;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.IntegrationTests.Infraestructura;

namespace Sgpla.IntegrationTests;

/// <summary>
/// La última defensa ante dos altas simultáneas: una violación de unicidad de PostgreSQL (23505) se responde como 409 y
/// solo se registra el nombre de la restricción, nunca el valor duplicado.
/// La carrera real no es reproducible por HTTP, así que la prueba provoca la violación directamente en la base
/// y se la entrega al manejador registrado en el host.
/// </summary>
public sealed class ViolacionUnicidadTests(PostgreSqlFixture baseDatos) : IAsyncDisposable
{
    private readonly SgplaApiFactory _api = new(baseDatos);

    [Fact]
    public async Task ViolacionDeUnicidad_Responde409ConCodigoYRegistraSoloLaRestriccion()
    {
        var nombre = DatosUnicos.Nombre("Duplicado");
        var excepcion = new DbUpdateException("Fallo al guardar.", await ProvocarViolacionAsync(nombre));
        var manejador = _api.Services.GetServices<IExceptionHandler>().OfType<ViolacionUnicidadExceptionHandler>().Single();
        await using var alcance = _api.Services.CreateAsyncScope();
        var contexto = new DefaultHttpContext { RequestServices = alcance.ServiceProvider };
        contexto.Response.Body = new MemoryStream();

        var manejada = await manejador.TryHandleAsync(contexto, excepcion, TestContext.Current.CancellationToken);

        manejada.ShouldBeTrue();
        contexto.Response.StatusCode.ShouldBe((int)HttpStatusCode.Conflict);
        contexto.Response.Body.Position = 0;
        var problema = await JsonSerializer.DeserializeAsync<JsonElement>(contexto.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
        problema.GetProperty("codigo").GetString().ShouldBe(ViolacionUnicidadExceptionHandler.Codigo);
        problema.GetRawText().ShouldNotContain(nombre);
        _api.Logs.Entradas.ShouldContain(entrada => entrada.Mensaje.Contains("uq_grado_academico__nombre", StringComparison.Ordinal));
        _api.Logs.Entradas.ShouldNotContain(entrada => entrada.Contiene(nombre));
    }

    [Fact]
    public void Persistencia_NoRegistraElFalloDeSaveChanges()
    {
        // SaveChangesFailed no puede provocarse de forma determinista sin una carrera; se verifica la configuración.
        using var alcance = _api.Services.CreateScope();
        var opciones = alcance.ServiceProvider.GetRequiredService<DbContextOptions<SgplaDbContext>>();

        var comportamiento = opciones.FindExtension<CoreOptionsExtension>()?.WarningsConfiguration
            .GetBehavior(CoreEventId.SaveChangesFailed);

        comportamiento.ShouldBe(WarningBehavior.Ignore);
    }

    [Fact]
    public async Task ErrorDeComandoEnEfCore_NoRegistraElValorDuplicado()
    {
        // CommandError sí se registra, pero sin la excepción ni los valores de los parámetros.
        var nombre = DatosUnicos.Nombre("Duplicado");
        await using var alcance = _api.Services.CreateAsyncScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<SgplaDbContext>();

        var excepcion = await Should.ThrowAsync<PostgresException>(() => contexto.Database.ExecuteSqlAsync(
            $"INSERT INTO academico.grado_academico (nombre) VALUES ({nombre}), ({nombre})",
            TestContext.Current.CancellationToken));

        excepcion.SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
        excepcion.ConstraintName.ShouldBe("uq_grado_academico__nombre");
        excepcion.MessageText.ShouldNotContain(nombre);
        (excepcion.Detail ?? string.Empty).ShouldNotContain(nombre);
        _api.Logs.Entradas.ShouldNotContain(entrada => entrada.Contiene(nombre), "EF Core registró el valor duplicado.");
    }

    [Fact]
    public async Task OtraExcepcion_NoLaManeja()
    {
        var manejador = _api.Services.GetServices<IExceptionHandler>().OfType<ViolacionUnicidadExceptionHandler>().Single();

        var manejada = await manejador.TryHandleAsync(
            new DefaultHttpContext(), new InvalidOperationException(), TestContext.Current.CancellationToken);

        manejada.ShouldBeFalse();
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private async Task<PostgresException> ProvocarViolacionAsync(string nombre)
    {
        await using var conexion = new NpgsqlConnection(baseDatos.CadenaConexion);
        await conexion.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new NpgsqlCommand(
            "INSERT INTO academico.grado_academico (nombre) VALUES (@nombre), (@nombre)", conexion);
        comando.Parameters.AddWithValue("@nombre", nombre);

        var excepcion = await Should.ThrowAsync<PostgresException>(() => comando.ExecuteNonQueryAsync(TestContext.Current.CancellationToken));
        excepcion.SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
        excepcion.ConstraintName.ShouldBe("uq_grado_academico__nombre");

        return excepcion;
    }
}
