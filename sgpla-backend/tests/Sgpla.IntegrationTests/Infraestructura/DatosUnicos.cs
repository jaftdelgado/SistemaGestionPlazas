using System.Globalization;

namespace Sgpla.IntegrationTests.Infraestructura;

/// <summary>
/// Valores únicos por prueba. Las pruebas comparten la base y corren en paralelo, así que cada una crea sus
/// propios datos y no depende de que una tabla esté vacía.
/// </summary>
public static class DatosUnicos
{
    private static int _contadorClavePeriodo = 99999;

    /// <summary>Un nombre con prefijo legible y un sufijo aleatorio de 32 caracteres hexadecimales.</summary>
    public static string Nombre(string prefijo) => $"{prefijo} {Guid.NewGuid():N}";

    /// <summary>Un número de artículo ya normalizado: 16 caracteres hexadecimales en mayúsculas.</summary>
    public static string Numero() => Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();

    /// <summary>Una clave entera positiva aleatoria.</summary>
    public static int ClaveEntera() => Random.Shared.Next(1, int.MaxValue);

    /// <summary>Una clave alfanumérica ya normalizada: los primeros 20 caracteres de un GUID, en mayúsculas.</summary>
    public static string ClaveAlfanumerica() => Guid.NewGuid().ToString("N")[..20].ToUpperInvariant();

    /// <summary>Un correo único bajo el dominio indicado: <c>u{guid:N}@{dominio}</c>.</summary>
    public static string Correo(string dominio) => $"u{Guid.NewGuid():N}@{dominio}";

    /// <summary>
    /// Una clave de periodo escolar: seis dígitos que empiezan en 100000 y crecen de uno en uno. Es única en toda la base
    /// compartida, porque el contenedor es nuevo en cada ejecución y la semilla no trae periodos.
    /// </summary>
    public static string ClavePeriodo() =>
        Interlocked.Increment(ref _contadorClavePeriodo).ToString("D6", CultureInfo.InvariantCulture);
}
