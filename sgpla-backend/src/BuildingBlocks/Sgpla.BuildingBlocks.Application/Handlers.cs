using Sgpla.SharedKernel;

namespace Sgpla.BuildingBlocks.Application;

/// <summary>Caso de uso que modifica el estado y no devuelve valor.</summary>
public interface ICommandHandler<in TCommand>
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

/// <summary>Caso de uso que modifica el estado y devuelve un valor.</summary>
public interface ICommandHandler<in TCommand, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

/// <summary>Caso de uso de solo lectura.</summary>
public interface IQueryHandler<in TQuery, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
