namespace Sgpla.SharedKernel;

/// <summary>Resultado de una operación sin valor: éxito o un <see cref="SharedKernel.Error"/>.</summary>
public class Result
{
    private static readonly Result Exito = new(null);

    private readonly Error? _error;

    protected Result(Error? error)
    {
        _error = error;
    }

    public bool IsSuccess => _error is null;

    public bool IsFailure => !IsSuccess;

    public Error Error => _error ?? throw new InvalidOperationException("Un resultado exitoso no tiene error.");

    public static Result Success() => Exito;

    /// <summary>
    /// Resultado exitoso con valor. Hace falta cuando el valor es una interfaz (<c>IReadOnlyList&lt;T&gt;</c>),
    /// porque C# no admite conversiones implícitas desde interfaces.
    /// </summary>
    public static Result<T> Success<T>(T value) => new(value);

    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(error);
    }

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>Resultado de una operación con valor: el valor o un <see cref="SharedKernel.Error"/>.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T value)
        : base(null)
    {
        _value = value;
    }

    private Result(Error error)
        : base(error)
    {
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Un resultado fallido no tiene valor.");

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(error);
    }
}
