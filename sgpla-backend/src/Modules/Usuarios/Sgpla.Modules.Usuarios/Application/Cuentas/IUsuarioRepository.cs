using Sgpla.Modules.Usuarios.Domain.Cuentas;

namespace Sgpla.Modules.Usuarios.Application.Cuentas;

/// <summary>Todos sus métodos ven solo cuentas activas, gracias al filtro de baja lógica.</summary>
internal interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Compara el correo ya normalizado; la columna ignora mayúsculas y acentos.</summary>
    Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken);

    Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken);

    Task<int> ContarSuperusuariosAsync(CancellationToken cancellationToken);

    void Agregar(Usuario usuario);
}
