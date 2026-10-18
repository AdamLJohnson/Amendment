namespace Amendment.Domain.Authorization;

/// <summary>Role names, identical to legacy <c>Amendment.Shared.RoleGroups</c>.</summary>
public static class Roles
{
    public const string Admin = "System Administrator";
    public const string ScreenController = "Screen Controller";
    public const string AmendmentEditor = "Amendment Editor";
    public const string Translator = "Translator";

    public static IReadOnlyList<string> All { get; } = [Admin, ScreenController, AmendmentEditor, Translator];
}

public enum Operation
{
    // Public (anonymous)
    ReadPublicPresentation,
    ReadPublicTimer,
    ReadPublicSettings,
    ReadLanguages,

    // Any authenticated user
    ListAmendments,
    ReadAmendment,
    ExportAmendments,
    ChangeOwnPassword,

    // Amendment editorial
    CreateAmendment,
    UpdateAmendment,
    ArchiveAmendment,
    CloneAmendment,
    ApproveAmendment,
    /// <summary>Approve with the legacy best-effort parent update (failures do not block approval).</summary>
    ApproveAmendmentBestEffort,
    DeleteAmendment,

    // Bodies (translations)
    CreateBody,
    UpdateBody,
    DeleteBody,

    // Screen and timer
    PresentAmendment,
    ClearPresentation,
    SetLanguagePresentation,
    ControlTimer,

    // Administration
    ManageSettings,
    ManageUsers,
    ManageRoles
}

/// <summary>Who is calling. Anonymous callers have no roles.</summary>
public sealed record Principal(string? UserId, IReadOnlySet<string> Roles)
{
    public static Principal Anonymous { get; } = new(null, new HashSet<string>());
    public bool IsAuthenticated => UserId is not null;

    public static Principal User(string userId, params string[] roles) => new(userId, new HashSet<string>(roles, StringComparer.Ordinal));
}

/// <summary>
/// Server-side role-to-operation matrix (decision D4: matches the legacy UI gates). Fails closed: unknown operations
/// and role names are denied.
/// </summary>
public static class RoleOperationMatrix
{
    private static readonly IReadOnlySet<string> AnyAuthenticated = new HashSet<string>();

    // null = public; empty set = any authenticated user; otherwise one of the roles is required.
    private static readonly Dictionary<Operation, IReadOnlySet<string>?> Rules = new()
    {
        [Operation.ReadPublicPresentation] = null,
        [Operation.ReadPublicTimer] = null,
        [Operation.ReadPublicSettings] = null,
        [Operation.ReadLanguages] = null,

        [Operation.ListAmendments] = AnyAuthenticated,
        [Operation.ReadAmendment] = AnyAuthenticated,
        [Operation.ExportAmendments] = AnyAuthenticated,
        [Operation.ChangeOwnPassword] = AnyAuthenticated,

        [Operation.CreateAmendment] = Set(Roles.Admin, Roles.AmendmentEditor),
        [Operation.UpdateAmendment] = Set(Roles.Admin, Roles.AmendmentEditor),
        [Operation.ArchiveAmendment] = Set(Roles.Admin, Roles.AmendmentEditor),
        [Operation.CloneAmendment] = Set(Roles.Admin, Roles.AmendmentEditor),
        [Operation.ApproveAmendment] = Set(Roles.Admin, Roles.AmendmentEditor),
        [Operation.ApproveAmendmentBestEffort] = Set(Roles.Admin),
        [Operation.DeleteAmendment] = Set(Roles.Admin),

        [Operation.CreateBody] = Set(Roles.Admin, Roles.AmendmentEditor, Roles.Translator),
        [Operation.UpdateBody] = Set(Roles.Admin, Roles.AmendmentEditor, Roles.Translator),
        [Operation.DeleteBody] = Set(Roles.Admin, Roles.AmendmentEditor, Roles.Translator),

        [Operation.PresentAmendment] = Set(Roles.Admin, Roles.ScreenController),
        [Operation.ClearPresentation] = Set(Roles.Admin, Roles.ScreenController),
        [Operation.SetLanguagePresentation] = Set(Roles.Admin, Roles.ScreenController),
        [Operation.ControlTimer] = Set(Roles.Admin, Roles.ScreenController),

        [Operation.ManageSettings] = Set(Roles.Admin),
        [Operation.ManageUsers] = Set(Roles.Admin),
        [Operation.ManageRoles] = Set(Roles.Admin)
    };

    public static bool IsPublic(Operation operation) => Rules.TryGetValue(operation, out var rule) && rule is null;

    public static bool IsAllowed(Principal principal, Operation operation)
    {
        if (!Rules.TryGetValue(operation, out var required))
            return false;
        if (required is null)
            return true;
        if (!principal.IsAuthenticated)
            return false;
        return required.Count == 0 || principal.Roles.Overlaps(required);
    }

    public static DomainError? Authorize(Principal principal, Operation operation) =>
        IsAllowed(principal, operation)
            ? null
            : DomainError.Forbidden(principal.IsAuthenticated ? $"You are not allowed to perform {operation}." : "Sign in required.");

    /// <summary>Operations each role can perform, for documentation and contract tests.</summary>
    public static IReadOnlyList<Operation> OperationsFor(Principal principal) =>
        Enum.GetValues<Operation>().Where(o => IsAllowed(principal, o)).ToList();

    private static IReadOnlySet<string> Set(params string[] roles) => new HashSet<string>(roles, StringComparer.Ordinal);
}
