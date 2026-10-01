namespace Sgpla.BuildingBlocks.Application;

/// <summary>
/// Almacenamiento externo de binarios. La base guarda solo la clave, el tamaño y el SHA-256 que devuelve
/// <see cref="GuardarAsync"/>; el contenido nunca va a SQL Server.
/// </summary>
public interface IAlmacenamientoArchivos
{
    /// <summary>Tamaño máximo de un archivo, en bytes, según la configuración.</summary>
    long TamanoMaximoBytes { get; }

    /// <summary>
    /// Guarda el contenido con una clave nueva, <c>{carpeta}/{guid}{extension}</c>, y calcula su tamaño y su SHA-256
    /// mientras lo escribe. Si falla a medias, no deja el archivo.
    /// </summary>
    Task<ArchivoGuardado> GuardarAsync(string carpeta, string extension, Stream contenido, CancellationToken cancellationToken);

    /// <summary>Abre el archivo para lectura; <c>null</c> si no existe.</summary>
    Task<Stream?> AbrirAsync(string clave, CancellationToken cancellationToken);

    /// <summary>
    /// Elimina el archivo si existe. No lanza excepciones de entrada y salida: si no puede eliminarlo, registra el archivo
    /// huérfano y devuelve <c>false</c>.
    /// </summary>
    Task<bool> IntentarEliminarAsync(string clave, CancellationToken cancellationToken);
}

/// <summary>Resultado de <see cref="IAlmacenamientoArchivos.GuardarAsync"/>.</summary>
public sealed record ArchivoGuardado(string Clave, long Tamano, ReadOnlyMemory<byte> ChecksumSha256);

/// <summary>
/// Archivo recibido en una petición, antes de guardarlo. <see cref="AbrirLectura"/> abre el contenido desde el inicio y
/// puede llamarse más de una vez.
/// </summary>
public sealed record ArchivoRecibido(string Nombre, string TipoContenido, long Tamano, Func<Stream> AbrirLectura)
{
    /// <summary>
    /// Lee hasta <paramref name="longitud"/> bytes desde el inicio del contenido; menos si el contenido es más corto.
    /// Sirve para comprobar la firma de un archivo sin cargarlo completo.
    /// </summary>
    public async Task<byte[]> LeerEncabezadoAsync(int longitud, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(longitud);

        await using var contenido = AbrirLectura();
        var encabezado = new byte[longitud];
        var leidos = await contenido.ReadAtLeastAsync(encabezado, longitud, throwOnEndOfStream: false, cancellationToken);
        return encabezado[..leidos];
    }
}
