using System.Text;
using Sgpla.BuildingBlocks.Application;

namespace Sgpla.UnitTests.BuildingBlocks;

public sealed class ArchivoRecibidoTests
{
    private static readonly byte[] Contenido = Encoding.ASCII.GetBytes("%PDF-1.7 contenido");

    [Fact]
    public async Task LeerEncabezadoAsync_ConContenidoMasLargo_DevuelveLosPrimerosBytes()
    {
        var archivo = Archivo(() => new MemoryStream(Contenido, writable: false));

        var encabezado = await archivo.LeerEncabezadoAsync(5, TestContext.Current.CancellationToken);

        encabezado.ShouldBe(Encoding.ASCII.GetBytes("%PDF-"));
    }

    [Fact]
    public async Task LeerEncabezadoAsync_ConContenidoMasCorto_DevuelveTodoElContenido()
    {
        var corto = Encoding.ASCII.GetBytes("%PD");
        var archivo = Archivo(() => new MemoryStream(corto, writable: false));

        var encabezado = await archivo.LeerEncabezadoAsync(5, TestContext.Current.CancellationToken);

        encabezado.ShouldBe(corto);
    }

    [Fact]
    public async Task LeerEncabezadoAsync_ConFlujoQueEntregaUnByteALaVez_DevuelveLosBytesPedidos()
    {
        var archivo = Archivo(() => new FlujoDeUnByte(Contenido));

        var encabezado = await archivo.LeerEncabezadoAsync(5, TestContext.Current.CancellationToken);

        encabezado.ShouldBe(Encoding.ASCII.GetBytes("%PDF-"));
    }

    [Fact]
    public async Task LeerEncabezadoAsync_LlamadoDosVeces_DevuelveLoMismoPorqueAbreDesdeElInicio()
    {
        var archivo = Archivo(() => new MemoryStream(Contenido, writable: false));

        var primero = await archivo.LeerEncabezadoAsync(5, TestContext.Current.CancellationToken);
        var segundo = await archivo.LeerEncabezadoAsync(5, TestContext.Current.CancellationToken);

        segundo.ShouldBe(primero);
    }

    [Fact]
    public async Task LeerEncabezadoAsync_ConLongitudCero_LanzaArgumentOutOfRangeException()
    {
        var archivo = Archivo(() => new MemoryStream(Contenido, writable: false));

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            archivo.LeerEncabezadoAsync(0, TestContext.Current.CancellationToken));
    }

    private static ArchivoRecibido Archivo(Func<Stream> abrir) =>
        new("oficio.pdf", "application/pdf", Contenido.LongLength, abrir);

    /// <summary>Entrega como máximo un byte por lectura, como un flujo de red.</summary>
    private sealed class FlujoDeUnByte(byte[] bytes) : MemoryStream(bytes, writable: false)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            base.ReadAsync(buffer[..Math.Min(1, buffer.Length)], cancellationToken);
    }
}
