using System.Text.RegularExpressions;

namespace Sgpla.SharedKernel;

/// <summary>Normalización de textos compartida por los módulos.</summary>
public static partial class Normalizacion
{
    /// <summary>"  Facultad   de  Letras " → "Facultad de Letras". <c>null</c> → "".</summary>
    public static string Texto(string? valor) => EspaciosRepetidos().Replace(valor?.Trim() ?? string.Empty, " ");

    /// <summary>Recorta; <c>null</c> → "". No quita separadores internos.</summary>
    public static string Recortar(string? valor) => valor?.Trim() ?? string.Empty;

    /// <summary>Solo caracteres ASCII '0' a '9' (no acepta dígitos de otros alfabetos).</summary>
    public static bool SonDigitos(string valor) => valor.All(c => c is >= '0' and <= '9');

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspaciosRepetidos();
}
