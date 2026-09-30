using System.ComponentModel.DataAnnotations;

namespace Sgpla.Modules.SolicitudesApertura.Infrastructure.Periodos;

internal sealed class PeriodosOptions
{
    public const string Seccion = "SolicitudesApertura";

    [Required]
    [RegularExpression("^[0-9]{6}$")]
    public string PeriodoActual { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^[0-9]{6}$")]
    public string PeriodoSiguiente { get; set; } = string.Empty;
}
