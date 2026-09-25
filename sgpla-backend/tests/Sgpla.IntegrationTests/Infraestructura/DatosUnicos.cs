namespace Sgpla.IntegrationTests.Infraestructura;

/// <summary>
/// Valores únicos por prueba. Las pruebas comparten la base y corren en paralelo, así que cada una crea sus
/// propios datos y no depende de que una tabla esté vacía (ESTANDAR_MODULOS.md, sección 12).
/// </summary>
public static class DatosUnicos
{
    /// <summary>Un nombre con prefijo legible y un sufijo aleatorio de 32 caracteres hexadecimales.</summary>
    public static string Nombre(string prefijo) => $"{prefijo} {Guid.NewGuid():N}";
}
