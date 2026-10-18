namespace Amendment.Domain;

/// <summary>Client-facing error codes (plan §5 error contract).</summary>
public enum DomainErrorCode
{
    ValidationError,
    NotFound,
    Forbidden,
    Conflict,
    PreconditionFailed,
    RateLimited,
    InternalError
}

public static class DomainErrorCodeExtensions
{
    /// <summary>Wire name, e.g. <c>VALIDATION_ERROR</c>.</summary>
    public static string ToWireName(this DomainErrorCode code) => code switch
    {
        DomainErrorCode.ValidationError => "VALIDATION_ERROR",
        DomainErrorCode.NotFound => "NOT_FOUND",
        DomainErrorCode.Forbidden => "FORBIDDEN",
        DomainErrorCode.Conflict => "CONFLICT",
        DomainErrorCode.PreconditionFailed => "PRECONDITION_FAILED",
        DomainErrorCode.RateLimited => "RATE_LIMITED",
        _ => "INTERNAL_ERROR"
    };
}

public sealed record DomainError(DomainErrorCode Code, string Message, string? Field = null)
{
    public static DomainError Validation(string field, string message) => new(DomainErrorCode.ValidationError, message, field);
    public static DomainError NotFound(string message) => new(DomainErrorCode.NotFound, message);
    public static DomainError Forbidden(string message) => new(DomainErrorCode.Forbidden, message);
    public static DomainError Conflict(string message) => new(DomainErrorCode.Conflict, message);
    public static DomainError Precondition(string message) => new(DomainErrorCode.PreconditionFailed, message);
}

public sealed class DomainResult<T>
{
    private readonly T? _value;

    private DomainResult(T? value, IReadOnlyList<DomainError> errors)
    {
        _value = value;
        Errors = errors;
    }

    public IReadOnlyList<DomainError> Errors { get; }
    public bool IsSuccess => Errors.Count == 0;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Result has errors: " + string.Join("; ", Errors.Select(e => e.Message)));

    public static DomainResult<T> Success(T value) => new(value, []);
    public static DomainResult<T> Failure(params DomainError[] errors) => Failure((IReadOnlyList<DomainError>)errors);

    public static DomainResult<T> Failure(IReadOnlyList<DomainError> errors)
    {
        if (errors.Count == 0)
            throw new ArgumentException("At least one error is required.", nameof(errors));
        return new(default, errors);
    }

    public static implicit operator DomainResult<T>(DomainError error) => Failure(error);
}
