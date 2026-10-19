using Amazon.CDK;
using Amendment.Cdk.Config;
using Amendment.Cdk.Stacks;

namespace Amendment.Cdk;

/// <summary>All stacks for one stage. Stack names are deterministic: <c>Amendment-{stage}-{Part}</c>.</summary>
public sealed record AmendmentStacks(DataStack Data, IdentityStack Identity, ApiStack Api, WebStack Web, ObservabilityStack Observability)
{
    public IEnumerable<Stack> All => [Data, Identity, Api, Web, Observability];

    public static AmendmentStacks Create(App app, StageConfig config, string? account)
    {
        Tags.Of(app).Add("Application", StageConfig.ApplicationTagValue);
        Tags.Of(app).Add("Environment", config.Stage);

        IStackProps Props(string part) => new StackProps
        {
            StackName = $"{config.Prefix}-{part}",
            Env = new Amazon.CDK.Environment { Account = account, Region = config.Region },
            TerminationProtection = config.IsProduction,
            Description = $"Amendment {config.Stage} {part.ToLowerInvariant()} resources"
        };

        var data = new DataStack(app, $"{config.Prefix}-Data", config, Props("Data"));
        var identity = new IdentityStack(app, $"{config.Prefix}-Identity", config, Props("Identity"));
        var api = new ApiStack(app, $"{config.Prefix}-Api", config, identity.UserPool, Props("Api"));
        var web = new WebStack(app, $"{config.Prefix}-Web", config, Props("Web"));
        var observability = new ObservabilityStack(app, $"{config.Prefix}-Observability", config, api.Api, Props("Observability"));

        return new AmendmentStacks(data, identity, api, web, observability);
    }
}
