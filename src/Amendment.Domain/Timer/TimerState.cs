using Amendment.Domain.Model;

namespace Amendment.Domain.Timer;

/// <summary>
/// Timestamp-based timer (<c>TIMER#MAIN</c>). Clients compute the countdown locally; the server never ticks.
/// Operations reproduce legacy <c>TimerService</c> semantics in whole seconds:
/// Reset() → 0 and stopped; Reset(s) → s and stopped; Set(s) keeps the running state; Start is ignored at 0 or
/// while running; reaching 0 stops the timer; Show is independent of the countdown.
/// </summary>
public sealed record TimerState
{
    public int RemainingAtLastChangeSeconds { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public bool Running { get; init; }
    public bool Visible { get; init; }
    public long Version { get; init; } = Versioning.InitialVersion;

    public static TimerState Initial { get; } = new();

    /// <summary><c>max(0, remainingAtLastChange - floor(elapsed))</c> while running; legacy decremented once per full second.</summary>
    public int RemainingSeconds(DateTimeOffset now)
    {
        if (!Running || StartedAt is null)
            return RemainingAtLastChangeSeconds;
        var elapsed = (long)Math.Floor(Math.Max(0, (now - StartedAt.Value).TotalSeconds));
        return (int)Math.Max(0, RemainingAtLastChangeSeconds - elapsed);
    }

    /// <summary>Running and not yet expired. A stored <c>Running=true</c> past zero is reported as stopped.</summary>
    public bool IsRunning(DateTimeOffset now) => Running && RemainingSeconds(now) > 0;

    public DomainResult<TimerState> Start(DateTimeOffset now)
    {
        var remaining = RemainingSeconds(now);
        if (remaining <= 0 || IsRunning(now))
            return Unchanged();
        return Next(this with { RemainingAtLastChangeSeconds = remaining, StartedAt = now, Running = true });
    }

    public DomainResult<TimerState> Pause(DateTimeOffset now)
    {
        if (!Running)
            return Unchanged();
        return Next(this with { RemainingAtLastChangeSeconds = RemainingSeconds(now), StartedAt = null, Running = false });
    }

    public DomainResult<TimerState> Reset(int seconds = 0)
    {
        if (seconds < 0)
            return DomainError.Validation("seconds", "Seconds cannot be negative.");
        return Next(this with { RemainingAtLastChangeSeconds = seconds, StartedAt = null, Running = false });
    }

    /// <summary>Sets the remaining time; a running timer keeps running from the new value.</summary>
    public DomainResult<TimerState> Set(int seconds, DateTimeOffset now)
    {
        if (seconds < 0)
            return DomainError.Validation("seconds", "Seconds cannot be negative.");
        var running = IsRunning(now) && seconds > 0;
        return Next(this with { RemainingAtLastChangeSeconds = seconds, StartedAt = running ? now : null, Running = running });
    }

    public DomainResult<TimerState> Show(bool visible) =>
        Visible == visible ? Unchanged() : Next(this with { Visible = visible });

    private DomainResult<TimerState> Unchanged() => DomainResult<TimerState>.Success(this);

    private DomainResult<TimerState> Next(TimerState updated) =>
        DomainResult<TimerState>.Success(updated with { Version = Versioning.Next(Version) });
}
