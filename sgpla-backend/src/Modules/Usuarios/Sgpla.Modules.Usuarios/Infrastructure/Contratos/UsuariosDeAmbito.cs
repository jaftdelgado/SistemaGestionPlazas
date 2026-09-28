using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Institucional.Application.Contracts;
using Sgpla.Modules.Usuarios.Domain.Cuentas;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Usuarios.Infrastructure.Contratos;

internal sealed class UsuariosDeAmbito(SgplaDbContext contexto) : IUsuariosDeAreaAcademica, IUsuariosDeEntidadAcademica
{
    public Task<bool> TieneUsuariosActivosAsync(int areaAcademicaId, CancellationToken cancellationToken) =>
        contexto.Set<Usuario>()
            .AnyAsync(u => u.Rol == Rol.Dgaa && u.PerfilDgaa!.AreaAcademicaId == areaAcademicaId, cancellationToken);

    Task<bool> IUsuariosDeEntidadAcademica.TieneUsuariosActivosAsync(int entidadAcademicaId, CancellationToken cancellationToken) =>
        contexto.Set<Usuario>().AnyAsync(
            u => u.Rol == Rol.EntidadAcademica && u.PerfilEntidadAcademica!.EntidadAcademicaId == entidadAcademicaId,
            cancellationToken);
}
