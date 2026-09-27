namespace Sgpla.IntegrationTests.Infraestructura;

/// <summary>
/// Valores únicos por prueba. Las pruebas comparten la base y corren en paralelo, así que cada una crea sus
/// propios datos y no depende de que una tabla esté vacía (ESTANDAR_MODULOS.md, sección 12).
/// </summary>
public static class DatosUnicos
{
    /// <summary>Un nombre con prefijo legible y un sufijo aleatorio de 32 caracteres hexadecimales.</summary>
    public static string Nombre(string prefijo) => $"{prefijo} {Guid.NewGuid():N}";

    /// <summary>Un número de artículo ya normalizado: 16 caracteres hexadecimales en mayúsculas.</summary>
    public static string Numero() => Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();

    /// <summary>Una clave entera positiva aleatoria.</summary>
    public static int ClaveEntera() => Random.Shared.Next(1, int.MaxValue);

    /// <summary>Una clave alfanumérica ya normalizada: los primeros 20 caracteres de un GUID, en mayúsculas.</summary>
    public static string ClaveAlfanumerica() => Guid.NewGuid().ToString("N")[..20].ToUpperInvariant();
}
