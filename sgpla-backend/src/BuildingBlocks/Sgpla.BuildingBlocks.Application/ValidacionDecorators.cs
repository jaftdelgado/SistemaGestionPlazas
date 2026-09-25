using FluentValidation;
using Sgpla.SharedKernel;

namespace Sgpla.BuildingBlocks.Application;

/// <summary>Ejecuta los validators del comando antes del handler. Si hay errores, no llama al handler.</summary>
public sealed class ValidacionCommandDecorator<TCommand>(
    ICommandHandler<TCommand> handler,
    IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand>
{
    public async Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        var error = await Validacion.ValidarAsync(validators, command, cancellationToken);
        return error ?? await handler.HandleAsync(command, cancellationToken);
    }
}

/// <summary>Ejecuta los validators del comando antes del handler. Si hay errores, no llama al handler.</summary>
public sealed class ValidacionCommandDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> handler,
    IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        var error = await Validacion.ValidarAsync(validators, command, cancellationToken);
        return error ?? await handler.HandleAsync(command, cancellationToken);
    }
}

/// <summary>Ejecuta los validators de la consulta antes del handler. Si hay errores, no llama al handler.</summary>
public sealed class ValidacionQueryDecorator<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> handler,
    IEnumerable<IValidator<TQuery>> validators) : IQueryHandler<TQuery, TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken)
    {
        var error = await Validacion.ValidarAsync(validators, query, cancellationToken);
        return error ?? await handler.HandleAsync(query, cancellationToken);
    }
}

internal static class Validacion
{
    public static async Task<ValidationError?> ValidarAsync<T>(
        IEnumerable<IValidator<T>> validators,
        T instancia,
        CancellationToken cancellationToken)
    {
        var fallas = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in validators)
        {
            var resultado = await validator.ValidateAsync(instancia, cancellationToken);
            fallas.AddRange(resultado.Errors);
        }

        if (fallas.Count == 0)
        {
            return null;
        }

        return new ValidationError(fallas
            .GroupBy(falla => falla.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo.Select(falla => falla.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal));
    }
}
