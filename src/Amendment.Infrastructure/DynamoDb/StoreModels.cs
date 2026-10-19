using Amendment.Domain.Languages;
using Amendment.Domain.Model;

namespace Amendment.Infrastructure.DynamoDb;

/// <summary>Supported language (<c>SYSTEM</c> / <c>LANGUAGE#&lt;code&gt;</c>). <c>LegacyCode</c> is the legacy <c>Language.LanguageCode</c>.</summary>
public sealed record LanguageRecord(LanguageCode Code, string Name, string LegacyCode, int SortOrder);

/// <summary>Nonsecret application setting (<c>SYSTEM</c> / <c>SETTING#&lt;key&gt;</c>).</summary>
public sealed record SettingRecord
{
    public required string Key { get; init; }
    public required string Value { get; init; }
    public long Version { get; init; } = Versioning.InitialVersion;
    public required AuditStamp Created { get; init; }
    public required AuditStamp Updated { get; init; }
}

public sealed record AmendmentWithBodies(AmendmentRecord Amendment, IReadOnlyList<AmendmentBodyRecord> Bodies);

/// <summary>One page of results. <c>NextPageToken</c> is null on the last page.</summary>
public sealed record Page<T>(IReadOnlyList<T> Items, string? NextPageToken);

/// <summary>Outcome of <see cref="AmendmentStore.TransactAsync"/>. <c>FailedWrite</c> is the write whose condition failed, when known.</summary>
public sealed record TransactionResult(Amendment.Domain.DomainError? Error, StoreWrite? FailedWrite)
{
    public static TransactionResult Committed { get; } = new(null, null);
    public bool IsCommitted => Error is null;
}

/// <summary>New amendment ids are ULIDs (contracts.md): sortable by creation time.</summary>
public static class AmendmentIds
{
    public static string New() => Ulid.NewUlid().ToString();
}
