using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Usuarios.Application.Cuentas;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Usuarios.Infrastructure.Cuentas;

internal sealed class UsuarioRepository(SgplaDbContext contexto) : IUsuarioRepository
{
    public Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        contexto.Set<Usuario>().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken) =>
        contexto.Set<Usuario>().FirstOrDefaultAsync(u => u.Correo == correo, cancellationToken);

    /// <summary>El correo solo es único entre cuentas activas (índice filtrado); no se usa IgnoreQueryFilters.</summary>
    public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken) =>
        contexto.Set<Usuario>().AnyAsync(u => u.Correo == correo, cancellationToken);

    public Task<int> ContarSuperusuariosAsync(CancellationToken cancellationToken) =>
        contexto.Set<Usuario>().CountAsync(u => u.Rol == Rol.Superusuario, cancellationToken);

    public void Agregar(Usuario usuario) => contexto.Set<Usuario>().Add(usuario);
}
