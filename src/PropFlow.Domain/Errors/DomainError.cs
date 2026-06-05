namespace PropFlow.Domain.Errors;

public sealed class DomainError : Exception
{
    public DomainErrorKind Kind { get; }

    private DomainError(DomainErrorKind kind, string message) : base(message)
        => Kind = kind;

    public static DomainError NotFound(string msg)     => new(DomainErrorKind.NotFound, msg);
    public static DomainError InvalidState(string msg) => new(DomainErrorKind.InvalidState, msg);
    public static DomainError Validation(string msg)   => new(DomainErrorKind.Validation, msg);
    public static DomainError Conflict(string msg)     => new(DomainErrorKind.Conflict, msg);
    public static DomainError Forbidden(string msg)    => new(DomainErrorKind.Forbidden, msg);
}

public enum DomainErrorKind { NotFound, InvalidState, Validation, Conflict, Forbidden }
