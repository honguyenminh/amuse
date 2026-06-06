using System.Diagnostics.CodeAnalysis;

namespace Amuse.Domain.SharedKernel;

public sealed class Result
{
    public bool IsSuccess { get; }
    public DomainError? Error { get; }

    private Result(bool isSuccess, DomainError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);

    public static Result Failure(DomainError error) => new(false, error);
}

[SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Result<T>.Success/Failure factory pattern.")]
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public DomainError? Error { get; }

    private Result(bool isSuccess, T? value, DomainError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);

    public static Result<T> Failure(DomainError error) => new(false, default, error);
}
