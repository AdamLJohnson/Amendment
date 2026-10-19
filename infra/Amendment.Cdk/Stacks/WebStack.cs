using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Amazon.CDK.AWS.Route53;
using Amazon.CDK.AWS.Route53.Targets;
using Amazon.CDK.AWS.S3;
using Amendment.Cdk.Config;
using Constructs;

namespace Amendment.Cdk.Stacks;

/// <summary>
/// Private S3 bucket behind CloudFront (OAC), ACM certificate (this stack must deploy to us-east-1) and Route 53 alias
/// records in the existing, shared hosted zone. The zone itself is imported and never created or deleted here.
/// </summary>
public sealed class WebStack : Stack
{
    public Bucket SiteBucket { get; }
    public Distribution Distribution { get; }

    public WebStack(Construct scope, string id, StageConfig config, IStackProps props) : base(scope, id, props)
    {
        var domain = config.Domain;
        var zone = HostedZone.FromHostedZoneAttributes(this, "SharedZone", new HostedZoneAttributes
        {
            HostedZoneId = domain.HostedZoneId,
            ZoneName = domain.HostedZoneName
        });

        var certificate = new Certificate(this, "SiteCertificate", new CertificateProps
        {
            DomainName = domain.SiteDomainName,
            Validation = CertificateValidation.FromDns(zone)
        });

        SiteBucket = new Bucket(this, "SiteBucket", new BucketProps
        {
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            Encryption = BucketEncryption.S3_MANAGED,
            EnforceSSL = true,
            ObjectOwnership = ObjectOwnership.BUCKET_OWNER_ENFORCED,
            Versioned = false,
            RemovalPolicy = config.IsProduction ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY
        });

        var spaFallback = new Func<double, ErrorResponse>(status => new ErrorResponse
        {
            HttpStatus = status,
            ResponseHttpStatus = 200,
            ResponsePagePath = "/index.html",
            Ttl = Duration.Seconds(0)
        });

        Distribution = new Distribution(this, "Distribution", new DistributionProps
        {
            Comment = $"{config.Prefix} web",
            DefaultBehavior = new BehaviorOptions
            {
                Origin = S3BucketOrigin.WithOriginAccessControl(SiteBucket),
                ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                CachePolicy = CachePolicy.CACHING_OPTIMIZED,
                ResponseHeadersPolicy = ResponseHeadersPolicy.SECURITY_HEADERS,
                Compress = true
            },
            DomainNames = [domain.SiteDomainName],
            Certificate = certificate,
            MinimumProtocolVersion = SecurityPolicyProtocol.TLS_V1_2_2021,
            HttpVersion = HttpVersion.HTTP2_AND_3,
            PriceClass = PriceClass.PRICE_CLASS_100,
            DefaultRootObject = "index.html",
            ErrorResponses = [spaFallback(403), spaFallback(404)]
        });

        var target = RecordTarget.FromAlias(new CloudFrontTarget(Distribution));
        _ = new ARecord(this, "SiteAliasA", new ARecordProps { Zone = zone, RecordName = domain.SiteDomainName, Target = target });
        _ = new AaaaRecord(this, "SiteAliasAAAA", new AaaaRecordProps { Zone = zone, RecordName = domain.SiteDomainName, Target = target });

        _ = new CfnOutput(this, "SiteBucketName", new CfnOutputProps { Value = SiteBucket.BucketName });
        _ = new CfnOutput(this, "DistributionId", new CfnOutputProps { Value = Distribution.DistributionId });
        _ = new CfnOutput(this, "DistributionDomainName", new CfnOutputProps { Value = Distribution.DistributionDomainName });
        _ = new CfnOutput(this, "SiteUrl", new CfnOutputProps { Value = config.SiteUrl });
    }
}
