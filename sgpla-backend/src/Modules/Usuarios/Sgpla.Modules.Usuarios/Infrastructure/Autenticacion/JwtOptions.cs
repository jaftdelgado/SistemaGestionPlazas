using System.ComponentModel.DataAnnotations;

namespace Sgpla.Modules.Usuarios.Infrastructure.Autenticacion;

internal sealed class JwtOptions
{
    public const string Seccion = "Jwt";

    [Required]
    public string Emisor { get; set; } = string.Empty;

    [Required]
    public string Audiencia { get; set; } = string.Empty;

    /// <summary>Nunca en <c>appsettings*.json</c>: llega por la variable de entorno <c>Jwt__Clave</c>.</summary>
    [Required]
    public string Clave { get; set; } = string.Empty;

    [Range(1, 24)]
    public int DuracionHoras { get; set; } = 8;
}
