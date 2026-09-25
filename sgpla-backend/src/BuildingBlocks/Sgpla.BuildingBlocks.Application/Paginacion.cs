using System.Text.Json.Serialization;
using FluentValidation;

namespace Sgpla.BuildingBlocks.Application;

/// <summary>Página solicitada de un listado. <see cref="Pagina"/> empieza en 1.</summary>
public sealed record Paginacion(int Pagina = 1, int TamanoPagina = Paginacion.TamanoPorOmision)
{
    public const int TamanoPorOmision = 20;

    public const int TamanoMaximo = 100;
}

/// <summary>Una página de un listado, con el total de elementos del listado completo.</summary>
public sealed record Pagina<T>(
    IReadOnlyList<T> Elementos,
    [property: JsonPropertyName("pagina")] int NumeroPagina,
    int TamanoPagina,
    int Total);

/// <summary>
/// Reglas de <see cref="Paginacion"/> para el validator de un listado. Los errores se reportan con los nombres
/// de los parámetros HTTP (<c>pagina</c> y <c>tamanoPagina</c>), no con la ruta de la propiedad.
/// </summary>
public sealed class PaginacionValidator<T> : AbstractValidator<T>
{
    public PaginacionValidator(Func<T, Paginacion> paginacion)
    {
        ArgumentNullException.ThrowIfNull(paginacion);

        RuleFor(x => paginacion(x).Pagina)
            .GreaterThanOrEqualTo(1)
            .OverridePropertyName(nameof(Paginacion.Pagina))
            .WithName("página");

        RuleFor(x => paginacion(x).TamanoPagina)
            .InclusiveBetween(1, Paginacion.TamanoMaximo)
            .OverridePropertyName(nameof(Paginacion.TamanoPagina))
            .WithName("tamaño de página");
    }
}
