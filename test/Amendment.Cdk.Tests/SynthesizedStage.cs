using System.Text.Json.Nodes;
using Amazon.CDK;
using Amazon.CDK.Assertions;
using Amendment.Cdk;
using Amendment.Cdk.Config;

// The jsii runtime behind CDK for .NET is not safe for concurrent use.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Amendment.Cdk.Tests;

/// <summary>Synthesizes all stacks for a stage into a temporary cloud assembly and exposes the templates.</summary>
public sealed class SynthesizedStage
{
    public const string TestAccount = "123456789012";

    public StageConfig Config { get; }
    public AmendmentStacks Stacks { get; }
    private readonly Dictionary<string, string> _templates = new();

    private SynthesizedStage(StageConfig config, AmendmentStacks stacks, string assemblyDir)
    {
        Config = config;
        Stacks = stacks;
        foreach (var stack in stacks.All)
            _templates[stack.StackName] = File.ReadAllText(Path.Combine(assemblyDir, stack.TemplateFile));
    }

    /// <summary>Stand-in for the published API Lambda folder; synthesis only needs the folder and the handler assembly name.</summary>
    private static readonly Lazy<string> FakeLambdaPackage = new(() =>
    {
        var dir = Path.Combine(Path.GetTempPath(), "amendment-cdk-tests", "api-lambda-package");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "Amendment.Api.Lambda.dll"), "placeholder");
        return dir;
    });

    public static SynthesizedStage Create(string stage, IDictionary<string, object>? context = null)
    {
        var ctx = new Dictionary<string, object>(context ?? new Dictionary<string, object>()) { ["stage"] = stage };
        ctx.TryAdd(Amendment.Cdk.Stacks.ApiStack.ApiLambdaPackageContextKey, FakeLambdaPackage.Value);
        var app = new App(new AppProps
        {
            Outdir = Path.Combine(Path.GetTempPath(), "amendment-cdk-tests", Guid.NewGuid().ToString("N")),
            Context = ctx
        });
        var config = StageConfig.Load(stage, app.Node);
        var stacks = AmendmentStacks.Create(app, config, TestAccount);
        var assembly = app.Synth();
        return new SynthesizedStage(config, stacks, assembly.Directory);
    }

    public Template Template(Stack stack) => Amazon.CDK.Assertions.Template.FromString(_templates[stack.StackName]);

    public JsonObject Json(Stack stack) => JsonNode.Parse(_templates[stack.StackName])!.AsObject();

    /// <summary>All resources across all stacks as (stack, logicalId, type, properties).</summary>
    public IEnumerable<(Stack Stack, string LogicalId, string Type, JsonObject Resource)> Resources() =>
        from stack in Stacks.All
        from r in Json(stack)["Resources"]!.AsObject()
        select (stack, r.Key, (string)r.Value!["Type"]!, r.Value!.AsObject());

    public static IReadOnlyDictionary<string, string> TagsOf(JsonObject resource)
    {
        var props = resource["Properties"] as JsonObject;
        var tags = props?["Tags"] ?? props?["UserPoolTags"];
        return tags switch
        {
            JsonArray list => list.ToDictionary(t => (string)t!["Key"]!, t => (string)t!["Value"]!),
            JsonObject map => map.ToDictionary(t => t.Key, t => (string)t.Value!),
            _ => new Dictionary<string, string>()
        };
    }
}
