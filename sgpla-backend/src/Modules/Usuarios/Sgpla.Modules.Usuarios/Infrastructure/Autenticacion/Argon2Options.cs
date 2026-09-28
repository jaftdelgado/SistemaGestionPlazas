using System.ComponentModel.DataAnnotations;

namespace Sgpla.Modules.Usuarios.Infrastructure.Autenticacion;

internal sealed class Argon2Options
{
    public const string Seccion = "Argon2";

    [Range(19456, int.MaxValue)]
    public int MemoriaKib { get; set; } = 19456;

    [Range(2, int.MaxValue)]
    public int Iteraciones { get; set; } = 2;

    [Range(1, int.MaxValue)]
    public int Paralelismo { get; set; } = 1;
}
