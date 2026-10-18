using Amendment.Domain.Languages;
using Amendment.Domain.Model;
using Amendment.Domain.Presentation;
using Amendment.Domain.Timer;

namespace Amendment.Domain.Events;

/// <summary>Event channels (plan §6). Only the public channels are anonymously subscribable; nobody publishes from a client.</summary>
public static class Channels
{
    public const string PublicPresentation = "/presentation/main";
    public const string PublicTimer = "/timer/main";

    /// <summary>Authenticated editorial channel for one amendment body.</summary>
    public static string Editorial(string amendmentId, LanguageCode language) => $"/amendments/{amendmentId}/{language.Value}";

    /// <summary>Authenticated editorial channel for amendment metadata.</summary>
    public static string EditorialMeta(string amendmentId) => $"/amendments/{amendmentId}/meta";
}

public static class EventTypes
{
    public const string AmendmentChanged = "amendment.changed";
    public const string AmendmentDeleted = "amendment.deleted";
    public const string BodyChanged = "body.changed";
    public const string BodyDeleted = "body.deleted";
    public const string PresentationChanged = "presentation.changed";
    public const string LiveBodyUpdated = "liveBody.updated";
    public const string TimerChanged = "timer.changed";
}

/// <summary>Private notification. Never carries markdown; clients re-fetch. <see cref="ItemVersion"/> is the changed item's own version.</summary>
public sealed record EditorialEvent(string Type, string AmendmentId, string? Language, long ItemVersion);

/// <summary>
/// Public notification. Carries only identifiers and versions; projectors always re-fetch <c>publicPresentation</c>.
/// <see cref="PresentationVersion"/> and <see cref="BodyVersion"/> are independent sequences.
/// </summary>
public sealed record PublicPresentationEvent(string Type, long PresentationVersion, string? Language = null, long? BodyVersion = null);

/// <summary>Public timer notification; payload equals the public timer read model.</summary>
public sealed record PublicTimerEvent(string Type, long Version, int RemainingAtLastChangeSeconds, DateTimeOffset? StartedAt, bool Running, bool Visible);

/// <summary>
/// Decides what the stream publisher sends. Public eligibility is re-evaluated against the authoritative
/// presentation state at publish time, never taken from the changed record alone.
/// </summary>
public static class EventPolicy
{
    public static EditorialEvent ForBody(AmendmentBodyRecord body, bool deleted = false) =>
        new(deleted ? EventTypes.BodyDeleted : EventTypes.BodyChanged, body.AmendmentId, body.Language.Value, body.Version);

    public static EditorialEvent ForAmendment(AmendmentRecord amendment, bool deleted = false) =>
        new(deleted ? EventTypes.AmendmentDeleted : EventTypes.AmendmentChanged, amendment.Id, null, amendment.Version);

    /// <summary>Public event for a committed body change, or null when the body is not currently visible.</summary>
    public static PublicPresentationEvent? PublicForBody(PresentationState current, AmendmentBodyRecord body) =>
        current.IsVisible(body.AmendmentId, body.Language)
            ? new PublicPresentationEvent(EventTypes.LiveBodyUpdated, current.Version, body.Language.Value, body.Version)
            : null;

    /// <summary>Every committed presentation change is public (it reveals only ids/versions).</summary>
    public static PublicPresentationEvent PublicForPresentation(PresentationState state) =>
        new(EventTypes.PresentationChanged, state.Version);

    public static PublicTimerEvent PublicForTimer(TimerState timer) =>
        new(EventTypes.TimerChanged, timer.Version, timer.RemainingAtLastChangeSeconds, timer.StartedAt, timer.Running, timer.Visible);
}
