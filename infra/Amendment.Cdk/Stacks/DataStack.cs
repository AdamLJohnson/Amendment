using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amendment.Cdk.Config;
using Constructs;
using Attribute = Amazon.CDK.AWS.DynamoDB.Attribute;

namespace Amendment.Cdk.Stacks;

/// <summary>Single application table (<c>AmendmentData-{stage}</c>), on-demand, PK/SK strings.</summary>
public sealed class DataStack : Stack
{
    /// <summary>Amendment listing in creation order (<c>GSI1PK=AMENDMENTS#ACTIVE|ARCHIVED</c>); only metadata items carry its keys.</summary>
    public const string AmendmentListIndex = "GSI1";

    public Table Table { get; }

    public DataStack(Construct scope, string id, StageConfig config, IStackProps props) : base(scope, id, props)
    {
        Table = new Table(this, "AmendmentData", new TableProps
        {
            TableName = $"AmendmentData-{config.Stage}",
            PartitionKey = new Attribute { Name = "PK", Type = AttributeType.STRING },
            SortKey = new Attribute { Name = "SK", Type = AttributeType.STRING },
            BillingMode = BillingMode.PAY_PER_REQUEST,
            Encryption = TableEncryption.DEFAULT,
            PointInTimeRecoverySpecification = new PointInTimeRecoverySpecification { PointInTimeRecoveryEnabled = config.IsProduction },
            DeletionProtection = config.IsProduction,
            Stream = StreamViewType.NEW_AND_OLD_IMAGES,
            RemovalPolicy = config.IsProduction ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY
        });

        Table.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
        {
            IndexName = AmendmentListIndex,
            PartitionKey = new Attribute { Name = "GSI1PK", Type = AttributeType.STRING },
            SortKey = new Attribute { Name = "GSI1SK", Type = AttributeType.STRING },
            ProjectionType = ProjectionType.ALL
        });

        _ = new CfnOutput(this, "TableName", new CfnOutputProps { Value = Table.TableName });
    }
}
