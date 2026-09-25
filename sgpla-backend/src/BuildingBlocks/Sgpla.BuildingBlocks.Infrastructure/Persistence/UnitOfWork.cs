using Sgpla.BuildingBlocks.Application;

namespace Sgpla.BuildingBlocks.Infrastructure.Persistence;

internal sealed class UnitOfWork(SgplaDbContext contexto) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) => contexto.SaveChangesAsync(cancellationToken);
}
