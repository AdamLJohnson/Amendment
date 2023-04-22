namespace Amendment.Shared.Responses;

public record SecondsUpdated(int Seconds);
public record StateUpdated(bool IsRunning);
public record ShowUpdated(bool show);
public record CurrentState(bool State, int Seconds, bool Show);