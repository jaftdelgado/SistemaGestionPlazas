using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Domain.Articulos;

internal static class ArticuloErrors
{
    public static readonly Error NumeroVacio = Error.Validation(
        "Articulo.NumeroVacio", "El número es obligatorio.", nameof(Articulo.Numero));

    public static readonly Error NumeroDemasiadoLargo = Error.Validation(
        "Articulo.NumeroDemasiadoLargo",
        $"El número admite hasta {Articulo.LongitudMaximaNumero} caracteres.",
        nameof(Articulo.Numero));

    public static readonly Error NumeroNoAscii = Error.Validation(
        "Articulo.NumeroNoAscii",
        "El número solo admite letras sin acentos, dígitos, espacios y signos ASCII.",
        nameof(Articulo.Numero));

    public static readonly Error DescripcionVacia = Error.Validation(
        "Articulo.DescripcionVacia", "La descripción es obligatoria.", nameof(Articulo.Descripcion));

    public static readonly Error DescripcionDemasiadoLarga = Error.Validation(
        "Articulo.DescripcionDemasiadoLarga",
        $"La descripción admite hasta {Articulo.LongitudMaximaDescripcion} caracteres.",
        nameof(Articulo.Descripcion));

    public static readonly Error NumeroDuplicado = Error.Conflict(
        "Articulo.NumeroDuplicado", "Ya existe un artículo con ese número.");

    public static readonly Error NumeroInmutable = Error.Conflict(
        "Articulo.NumeroInmutable", "El número ya no puede modificarse porque un Aviso usa el artículo.");

    public static Error NoEncontrado(int id) => Error.NotFound(
        "Articulo.NoEncontrado", $"No existe el artículo {id}.");
}
