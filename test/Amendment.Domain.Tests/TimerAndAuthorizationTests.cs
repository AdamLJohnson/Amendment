using Amendment.Domain.Authorization;
using Amendment.Domain.Timer;
using static Amendment.Domain.Tests.Fixtures;

namespace Amendment.Domain.Tests;

public class TimerStateTests
{
    [Fact]
    public void Running_CountsDownByWholeSeconds_AndStopsAtZero()
    {
        var timer = Timers.Running300;
        Assert.Equal(300, timer.RemainingSeconds(T0.AddMilliseconds(999)));
        Assert.Equal(299, timer.RemainingSeconds(T0.AddSeconds(1)));
        Assert.Equal(0, timer.RemainingSeconds(T0.AddHours(1)));
        Assert.False(timer.IsRunning(T0.AddSeconds(300)));
        Assert.Equal(300, timer.RemainingSeconds(T0.AddSeconds(-5)));
    }

    [Fact]
    public void Pause_PersistsRemaining()
    {
        var paused = Timers.Running300.Pause(T0.AddSeconds(42.7)).Value;
        Assert.Equal((258, false, (DateTimeOffset?)null, 8L), (paused.RemainingAtLastChangeSeconds, paused.Running, paused.StartedAt, paused.Version));
        Assert.Same(paused, paused.Pause(T0.AddSeconds(50)).Value);
    }

    [Fact]
    public void Start_IgnoredAtZeroOrWhileRunning()
    {
        Assert.Same(Timers.Stopped, Timers.Stopped.Start(T0).Value);
        Assert.Same(Timers.Running300, Timers.Running300.Start(T0.AddSeconds(10)).Value);

        var started = Timers.Paused90.Start(T0).Value;
        Assert.True(started.IsRunning(T0));
        Assert.Equal(80, started.RemainingSeconds(T0.AddSeconds(10)));
    }

    [Fact]
    public void Reset_StopsTimer_DefaultsToZero()
    {
        Assert.Equal((0, false), (Timers.Running300.Reset().Value.RemainingAtLastChangeSeconds, Timers.Running300.Reset().Value.Running));
        Assert.Equal(120, Timers.Running300.Reset(120).Value.RemainingSeconds(T0.AddSeconds(30)));
        Assert.Equal(DomainErrorCode.ValidationError, Timers.Stopped.Reset(-1).Errors.Single().Code);
    }

    [Fact]
    public void Set_KeepsRunningState()
    {
        var running = Timers.Running300.Set(60, T0.AddSeconds(10)).Value;
        Assert.True(running.IsRunning(T0.AddSeconds(10)));
        Assert.Equal(55, running.RemainingSeconds(T0.AddSeconds(15)));

        var paused = Timers.Paused90.Set(30, T0).Value;
        Assert.False(paused.Running);
        Assert.Equal(30, paused.RemainingSeconds(T0.AddSeconds(100)));
    }

    [Fact]
    public void Show_IsIndependent_AndIdempotent()
    {
        var shown = Timers.Paused90.Show(true).Value;
        Assert.True(shown.Visible);
        Assert.Equal(90, shown.RemainingAtLastChangeSeconds);
        Assert.Same(shown, shown.Show(true).Value);
    }
}

public class RoleOperationMatrixTests
{
    public static TheoryData<string, Operation, bool> Cases => new()
    {
        { "anon", Operation.ReadPublicPresentation, true },
        { "anon", Operation.ReadPublicTimer, true },
        { "anon", Operation.ListAmendments, false },
        { "anon", Operation.ReadAmendment, false },
        { "anon", Operation.ExportAmendments, false },
        { "anon", Operation.ControlTimer, false },
        { "anon", Operation.ManageSettings, false },
        { "nobody", Operation.ListAmendments, true },
        { "nobody", Operation.UpdateBody, false },
        { "tr", Operation.UpdateBody, true },
        { "tr", Operation.CreateBody, true },
        { "tr", Operation.UpdateAmendment, false },
        { "tr", Operation.ApproveAmendment, false },
        { "tr", Operation.DeleteAmendment, false },
        { "tr", Operation.SetLanguagePresentation, false },
        { "tr", Operation.ManageSettings, false },
        { "ae", Operation.CreateAmendment, true },
        { "ae", Operation.ApproveAmendment, true },
        { "ae", Operation.ApproveAmendmentBestEffort, false },
        { "ae", Operation.CloneAmendment, true },
        { "ae", Operation.UpdateBody, true },
        { "ae", Operation.DeleteAmendment, false },
        { "ae", Operation.PresentAmendment, false },
        { "sc", Operation.PresentAmendment, true },
        { "sc", Operation.ControlTimer, true },
        { "sc", Operation.UpdateBody, false },
        { "sc", Operation.ApproveAmendment, false },
        { "admin", Operation.DeleteAmendment, true },
        { "admin", Operation.ApproveAmendmentBestEffort, true },
        { "admin", Operation.ManageUsers, true }
    };

    private static Principal For(string id) => id switch
    {
        "anon" => Principal.Anonymous,
        "nobody" => NoRoles,
        "tr" => Translator,
        "ae" => AmendEditor,
        "sc" => ScreenController,
        _ => Admin
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Matrix(string principal, Operation operation, bool allowed) =>
        Assert.Equal(allowed, RoleOperationMatrix.IsAllowed(For(principal), operation));

    [Fact]
    public void Admin_CanDoEverything() =>
        Assert.Equal(Enum.GetValues<Operation>(), RoleOperationMatrix.OperationsFor(Admin));

    [Fact]
    public void EveryOperationHasARule_AndUnknownRolesAreDenied()
    {
        var stranger = Principal.User("x", "system administrator", "Superuser");
        foreach (var op in Enum.GetValues<Operation>())
            Assert.Equal(RoleOperationMatrix.IsPublic(op) || RoleOperationMatrix.IsAllowed(NoRoles, op), RoleOperationMatrix.IsAllowed(stranger, op));
    }
}
