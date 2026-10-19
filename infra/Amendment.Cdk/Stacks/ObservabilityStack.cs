using Amazon.CDK;
using Amazon.CDK.AWS.AppSync;
using Amazon.CDK.AWS.Budgets;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.CloudWatch.Actions;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SNS.Subscriptions;
using Amendment.Cdk.Config;
using Constructs;

namespace Amendment.Cdk.Stacks;

/// <summary>Alert topic, AppSync server-error alarm and a monthly cost budget filtered on the Application tag.</summary>
public sealed class ObservabilityStack : Stack
{
    public Topic AlertTopic { get; }

    public ObservabilityStack(Construct scope, string id, StageConfig config, IGraphqlApi api, IStackProps props) : base(scope, id, props)
    {
        var hasEmail = !string.IsNullOrWhiteSpace(config.AlertEmail);

        AlertTopic = new Topic(this, "AlertTopic", new TopicProps { TopicName = $"{config.Prefix}-alerts", EnforceSSL = true });
        if (hasEmail)
            AlertTopic.AddSubscription(new EmailSubscription(config.AlertEmail));

        var apiErrors = new Alarm(this, "ApiServerErrors", new AlarmProps
        {
            AlarmName = $"{config.Prefix}-api-5xx",
            AlarmDescription = "AppSync returned server (5XX) errors.",
            Metric = new Metric(new MetricProps
            {
                Namespace = "AWS/AppSync",
                MetricName = "5XXError",
                DimensionsMap = new Dictionary<string, string> { ["GraphQLAPIId"] = api.ApiId },
                Statistic = Stats.SUM,
                Period = Duration.Minutes(5)
            }),
            Threshold = 1,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
            TreatMissingData = TreatMissingData.NOT_BREACHING
        });
        apiErrors.AddAlarmAction(new SnsAction(AlertTopic));

        _ = new CfnBudget(this, "MonthlyBudget", new CfnBudgetProps
        {
            Budget = new CfnBudget.BudgetDataProperty
            {
                BudgetName = $"{config.Prefix}-monthly",
                BudgetType = "COST",
                TimeUnit = "MONTHLY",
                BudgetLimit = new CfnBudget.SpendProperty { Amount = config.MonthlyBudgetUsd, Unit = "USD" },
                CostFilters = new Dictionary<string, object>
                {
                    ["TagKeyValue"] = new[] { $"user:Application${StageConfig.ApplicationTagValue}" }
                }
            },
            NotificationsWithSubscribers = hasEmail
                ? new[] { Notification(80, "ACTUAL", config.AlertEmail), Notification(100, "FORECASTED", config.AlertEmail) }
                : null
        });
    }

    private static CfnBudget.NotificationWithSubscribersProperty Notification(double percent, string type, string email) => new()
    {
        Notification = new CfnBudget.NotificationProperty
        {
            ComparisonOperator = "GREATER_THAN",
            NotificationType = type,
            Threshold = percent,
            ThresholdType = "PERCENTAGE"
        },
        Subscribers = new[] { new CfnBudget.SubscriberProperty { Address = email, SubscriptionType = "EMAIL" } }
    };
}
