namespace Sgpla.BuildingBlocks.Application;

/// <summary>Guarda de forma atómica todo lo que modificó un caso de uso. El handler lo llama una sola vez, al final.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
