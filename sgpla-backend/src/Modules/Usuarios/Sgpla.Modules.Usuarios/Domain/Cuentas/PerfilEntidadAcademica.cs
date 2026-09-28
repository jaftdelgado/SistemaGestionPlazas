namespace Sgpla.Modules.Usuarios.Domain.Cuentas;

/// <summary>Parte 1:1 del agregado <see cref="Usuario"/> exclusiva del rol <c>EntidadAcademica</c> (DATABASE.md §6.19).</summary>
internal sealed class PerfilEntidadAcademica
{
    private PerfilEntidadAcademica()
    {
    }

    public int EntidadAcademicaId { get; private set; }

    /// <summary>El id no se valida aquí: su existencia y vigencia las comprueba el handler.</summary>
    public static PerfilEntidadAcademica Crear(int entidadAcademicaId) => new() { EntidadAcademicaId = entidadAcademicaId };
}
