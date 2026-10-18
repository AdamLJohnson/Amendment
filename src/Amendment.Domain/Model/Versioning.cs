namespace Amendment.Domain.Model;

/// <summary>
/// Per-item optimistic concurrency (plan §4). Versions are 64-bit, start at 1 on create, and increase by 1 per
/// successful mutation of that item only. Versions of different items are never compared.
/// </summary>
public static class Versioning
{
    public const long InitialVersion = 1;

    public static long Next(long current) => checked(current + 1);

    /// <summary>
    /// Compare-and-swap precondition. <paramref name="expected"/> null means "no check" and is only valid
    /// for operations the contract marks as last-writer-wins.
    /// </summary>
    public static DomainError? Check(string itemDescription, long current, long? expected)
    {
        if (expected is null || expected.Value == current)
            return null;
        return DomainError.Conflict($"{itemDescription} was changed by someone else (expected version {expected}, current {current}).");
    }
}
