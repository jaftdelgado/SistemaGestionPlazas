using System.ComponentModel.DataAnnotations;

namespace Sgpla.BuildingBlocks.Infrastructure.Archivos;

public sealed class AlmacenamientoOptions
{
    public const string Seccion = "Almacenamiento";

    [Required]
    public string RutaBase { get; set; } = string.Empty;

    [Range(1, 26_214_400)]
    public long TamanoMaximoBytes { get; set; } = 10_485_760;
}
