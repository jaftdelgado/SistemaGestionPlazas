using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sgpla.BuildingBlocks.Application;

namespace Sgpla.BuildingBlocks.Infrastructure.Archivos;

public sealed class AlmacenamientoLocal(
    IOptions<AlmacenamientoOptions> opciones,
    ILogger<AlmacenamientoLocal> logger) : IAlmacenamientoArchivos
{
    private static readonly StringComparison ComparacionRutas =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private string RutaBase => Path.GetFullPath(opciones.Value.RutaBase);

    public long TamanoMaximoBytes => opciones.Value.TamanoMaximoBytes;

    public async Task<ArchivoGuardado> GuardarAsync(
        string carpeta,
        string extension,
        Stream contenido,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(carpeta);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);
        ArgumentNullException.ThrowIfNull(contenido);

        var nombre = $"{Guid.NewGuid():N}{extension}";
        var clave = $"{carpeta.Trim('/', '\\').Replace('\\', '/')}/{nombre}";
        var ruta = Resolver(clave);
        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);

        try
        {
            await using var destino = new FileStream(
                ruta, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81_920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[81_920];
            long tamano = 0;

            while (true)
            {
                var leidos = await contenido.ReadAsync(buffer.AsMemory(), cancellationToken);
                if (leidos == 0)
                {
                    break;
                }

                await destino.WriteAsync(buffer.AsMemory(0, leidos), cancellationToken);
                hash.AppendData(buffer, 0, leidos);
                tamano += leidos;
            }

            await destino.FlushAsync(cancellationToken);
            return new ArchivoGuardado(clave, tamano, hash.GetHashAndReset());
        }
        catch
        {
            if (File.Exists(ruta))
            {
                File.Delete(ruta);
            }

            throw;
        }
    }

    public Task<Stream?> AbrirAsync(string clave, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ruta = Resolver(clave);

        if (!File.Exists(ruta))
        {
            return Task.FromResult<Stream?>(null);
        }

        try
        {
            Stream archivo = new FileStream(
                ruta, FileMode.Open, FileAccess.Read, FileShare.Read, 81_920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            return Task.FromResult<Stream?>(archivo);
        }
        catch (FileNotFoundException)
        {
            return Task.FromResult<Stream?>(null);
        }
    }

    public Task<bool> IntentarEliminarAsync(string clave, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ruta = Resolver(clave);

        try
        {
            File.Delete(ruta);
            return Task.FromResult(true);
        }
        catch (IOException)
        {
            logger.ArchivoHuerfano(clave);
            return Task.FromResult(false);
        }
        catch (UnauthorizedAccessException)
        {
            logger.ArchivoHuerfano(clave);
            return Task.FromResult(false);
        }
    }

    private string Resolver(string clave)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clave);

        var baseAbsoluta = RutaBase;
        var relativo = clave.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var ruta = Path.GetFullPath(Path.Combine(baseAbsoluta, relativo));
        var prefijo = baseAbsoluta.EndsWith(Path.DirectorySeparatorChar)
            ? baseAbsoluta
            : baseAbsoluta + Path.DirectorySeparatorChar;

        if (!ruta.StartsWith(prefijo, ComparacionRutas))
        {
            throw new ArgumentException("La clave debe apuntar a un archivo dentro de la ruta base.", nameof(clave));
        }

        return ruta;
    }
}

internal static partial class AlmacenamientoLocalLog
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No se pudo eliminar el archivo {ArchivoClave}; quedó huérfano en el almacenamiento")]
    public static partial void ArchivoHuerfano(this ILogger logger, string archivoClave);
}
