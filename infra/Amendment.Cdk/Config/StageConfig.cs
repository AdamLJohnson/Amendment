using System.Text.Json;
using Constructs;

namespace Amendment.Cdk.Config;

/// <summary>Nonsecret domain settings. The hosted zone is an existing shared resource and is never created by CDK.</summary>
public sealed record StageDomainConfig(string HostedZoneName, string HostedZoneId, string SiteDomainName);

/// <summary>Typed, nonsecret configuration for one deployment stage.</summary>
public sealed record StageConfig(
    string Stage,
    string Region,
    bool IsProduction,
    StageDomainConfig Domain,
    int LogRetentionDays,
    int MonthlyBudgetUsd,
    string AlertEmail,
    IReadOnlyList<string> AdditionalCallbackUrls)
{
    public const string ApplicationTagValue = "Amendment";

    /// <summary>Prefix for stack and resource names, e.g. <c>Amendment-dev</c>.</summary>
    public string Prefix => $"{ApplicationTagValue}-{Stage}";

    public string SiteUrl => $"https://{Domain.SiteDomainName}";

    /// <summary>Custom domain of the Cognito sign-in pages, e.g. <c>auth.dev-amendments.adamlj.com</c>.</summary>
    public string SignInDomainName => $"auth.{Domain.SiteDomainName}";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed record StageFile(
        string Region,
        bool IsProduction,
        StageDomainConfig Domain,
        int LogRetentionDays,
        int MonthlyBudgetUsd,
        string? AlertEmail,
        string[]? AdditionalCallbackUrls);

    /// <summary>
    /// Loads <paramref name="stage"/> from <c>Config/stages.json</c>. CDK context values override file values:
    /// <c>hostedZoneName</c>, <c>hostedZoneId</c>, <c>siteDomainName</c>, <c>alertEmail</c>, <c>monthlyBudgetUsd</c>.
    /// </summary>
    public static StageConfig Load(string stage, Node? contextNode = null, string? configPath = null)
    {
        configPath ??= Path.Combine(AppContext.BaseDirectory, "Config", "stages.json");
        var stages = JsonSerializer.Deserialize<Dictionary<string, StageFile>>(File.ReadAllText(configPath), JsonOptions)
                     ?? throw new InvalidOperationException($"Could not read {configPath}.");
        if (!stages.TryGetValue(stage, out var file))
            throw new ArgumentException($"Unknown stage '{stage}'. Known stages: {string.Join(", ", stages.Keys)}.", nameof(stage));

        string? Ctx(string key) => contextNode?.TryGetContext(key) as string is { Length: > 0 } v ? v : null;

        var domain = new StageDomainConfig(
            Ctx("hostedZoneName") ?? file.Domain.HostedZoneName,
            Ctx("hostedZoneId") ?? file.Domain.HostedZoneId,
            Ctx("siteDomainName") ?? file.Domain.SiteDomainName);

        var config = new StageConfig(
            stage,
            file.Region,
            file.IsProduction,
            domain,
            file.LogRetentionDays,
            int.TryParse(Ctx("monthlyBudgetUsd"), out var budget) ? budget : file.MonthlyBudgetUsd,
            Ctx("alertEmail") ?? file.AlertEmail ?? "",
            file.AdditionalCallbackUrls ?? []);

        config.Validate();
        return config;
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Domain.HostedZoneName) || string.IsNullOrWhiteSpace(Domain.HostedZoneId))
            throw new InvalidOperationException($"Stage '{Stage}': hostedZoneName and hostedZoneId are required.");
        var site = Domain.SiteDomainName.TrimEnd('.');
        var zone = Domain.HostedZoneName.TrimEnd('.');
        if (site.Equals(zone, StringComparison.OrdinalIgnoreCase) || !site.EndsWith("." + zone, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Stage '{Stage}': siteDomainName '{site}' must be a subdomain of '{zone}'.");
        if (MonthlyBudgetUsd <= 0)
            throw new InvalidOperationException($"Stage '{Stage}': monthlyBudgetUsd must be positive.");
    }
}
