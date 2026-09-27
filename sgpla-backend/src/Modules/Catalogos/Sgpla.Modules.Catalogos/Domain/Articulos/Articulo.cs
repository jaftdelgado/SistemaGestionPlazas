using System.Text.RegularExpressions;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Domain.Articulos;

/// <summary>
/// Artículo que fundamenta un Aviso (DATABASE.md §15.2). El número es una referencia opaca (<c>42</c>, <c>42 BIS</c>)
/// que queda inmutable cuando un Aviso usa el artículo; la descripción es obligatoria y siempre editable.
/// </summary>
internal sealed partial class Articulo : Entity
{
    public const int LongitudMaximaNumero = 50;
    public const int LongitudMaximaDescripcion = 1000;

    private Articulo()
    {
    }

    public string Numero { get; private set; } = string.Empty;

    public string Descripcion { get; private set; } = string.Empty;

    public static Result<Articulo> Crear(string numero, string descripcion)
    {
        var numeroNormalizado = NormalizarNumero(numero);
        if (numeroNormalizado.IsFailure)
        {
            return numeroNormalizado.Error;
        }

        var descripcionNormalizada = NormalizarDescripcion(descripcion);
        if (descripcionNormalizada.IsFailure)
        {
            return descripcionNormalizada.Error;
        }

        return new Articulo { Numero = numeroNormalizado.Value, Descripcion = descripcionNormalizada.Value };
    }

    /// <summary>
    /// Modifica el número y, si llega, la descripción; una descripción <c>null</c> conserva la actual. Si el número
    /// puede cambiar lo decide el handler, porque depende de si algún Aviso usa el artículo.
    /// </summary>
    public Result Modificar(string numero, string? descripcion)
    {
        var numeroNormalizado = NormalizarNumero(numero);
        if (numeroNormalizado.IsFailure)
        {
            return numeroNormalizado.Error;
        }

        Result<string> descripcionNormalizada = descripcion is null ? Descripcion : NormalizarDescripcion(descripcion);
        if (descripcionNormalizada.IsFailure)
        {
            return descripcionNormalizada.Error;
        }

        Numero = numeroNormalizado.Value;
        Descripcion = descripcionNormalizada.Value;
        return Result.Success();
    }

    /// <summary>"  42  bis " → "42 BIS": sin espacios exteriores, espacios internos simples, ASCII imprimible y en mayúsculas.</summary>
    private static Result<string> NormalizarNumero(string? numero)
    {
        var colapsado = EspaciosRepetidos().Replace(numero?.Trim() ?? string.Empty, " ");

        if (colapsado.Length == 0)
        {
            return ArticuloErrors.NumeroVacio;
        }

        if (colapsado.Length > LongitudMaximaNumero)
        {
            return ArticuloErrors.NumeroDemasiadoLargo;
        }

        if (!colapsado.All(caracter => caracter is >= ' ' and <= '~'))
        {
            return ArticuloErrors.NumeroNoAscii;
        }

        return colapsado.ToUpperInvariant();
    }

    private static Result<string> NormalizarDescripcion(string? descripcion)
    {
        var recortada = descripcion?.Trim() ?? string.Empty;

        if (recortada.Length == 0)
        {
            return ArticuloErrors.DescripcionVacia;
        }

        if (recortada.Length > LongitudMaximaDescripcion)
        {
            return ArticuloErrors.DescripcionDemasiadoLarga;
        }

        return recortada;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspaciosRepetidos();
}
