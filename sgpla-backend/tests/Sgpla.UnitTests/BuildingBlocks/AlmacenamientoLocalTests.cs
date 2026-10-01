using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sgpla.BuildingBlocks.Infrastructure.Archivos;

namespace Sgpla.UnitTests.BuildingBlocks;

public sealed class AlmacenamientoLocalTests : IAsyncDisposable
{
    private readonly string _rutaBase = Path.Combine(Path.GetTempPath(), "sgpla-archivos-unit", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task GuardarAsync_DevuelveClaveTamanoYChecksumDelContenido()
    {
        var bytes = "%PDF-prueba"u8.ToArray();
        await using var contenido = new MemoryStream(bytes);

        var guardado = await Almacenamiento().GuardarAsync("solicitudes-apertura", ".pdf", contenido, CancellationToken.None);

        guardado.Clave.ShouldStartWith("solicitudes-apertura/");
        guardado.Clave.ShouldEndWith(".pdf");
        guardado.Tamano.ShouldBe(bytes.LongLength);
        guardado.ChecksumSha256.ToArray().ShouldBe(SHA256.HashData(bytes));
        File.ReadAllBytes(Path.Combine(_rutaBase, guardado.Clave.Replace('/', Path.DirectorySeparatorChar))).ShouldBe(bytes);
    }

    [Fact]
    public async Task AbrirAsync_ConClaveInexistente_DevuelveNull()
    {
        var archivo = await Almacenamiento().AbrirAsync("solicitudes-apertura/inexistente.pdf", CancellationToken.None);

        archivo.ShouldBeNull();
    }

    [Fact]
    public async Task IntentarEliminarAsync_ConArchivoInexistente_DevuelveTrue()
    {
        var eliminado = await Almacenamiento().IntentarEliminarAsync(
            "solicitudes-apertura/inexistente.pdf", CancellationToken.None);

        eliminado.ShouldBeTrue();
    }

    [Fact]
    public async Task AbrirAsync_ConClaveQueSaleDeRutaBase_LanzaArgumentException()
    {
        await Should.ThrowAsync<ArgumentException>(() =>
            Almacenamiento().AbrirAsync("../fuera.pdf", CancellationToken.None));
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(_rutaBase))
        {
            Directory.Delete(_rutaBase, recursive: true);
        }

        return ValueTask.CompletedTask;
    }

    private AlmacenamientoLocal Almacenamiento()
    {
        Directory.CreateDirectory(_rutaBase);
        return new AlmacenamientoLocal(
            Options.Create(new AlmacenamientoOptions { RutaBase = _rutaBase }),
            NullLogger<AlmacenamientoLocal>.Instance);
    }
}
