using Amazon.CDK;
using Amendment.Cdk;
using Amendment.Cdk.Config;

var app = new App();
var stage = app.Node.TryGetContext("stage") as string ?? "dev";
var config = StageConfig.Load(stage, app.Node);

AmendmentStacks.Create(app, config, System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"));

app.Synth();
