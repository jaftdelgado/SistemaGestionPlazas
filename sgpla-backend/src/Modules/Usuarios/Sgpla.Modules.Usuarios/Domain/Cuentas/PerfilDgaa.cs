namespace Sgpla.Modules.Usuarios.Domain.Cuentas;

/// <summary>Parte 1:1 del agregado <see cref="Usuario"/> exclusiva del rol <c>Dgaa</c> (DATABASE.md §6.18).</summary>
internal sealed class PerfilDgaa
{
    private PerfilDgaa()
    {
    }

    public int AreaAcademicaId { get; private set; }

    /// <summary>El id no se valida aquí: su existencia y vigencia las comprueba el handler.</summary>
    public static PerfilDgaa Crear(int areaAcademicaId) => new() { AreaAcademicaId = areaAcademicaId };
}
