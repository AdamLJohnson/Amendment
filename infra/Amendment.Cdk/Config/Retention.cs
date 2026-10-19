using Amazon.CDK.AWS.Logs;

namespace Amendment.Cdk.Config;

public static class Retention
{
    public static RetentionDays FromDays(int days) => days switch
    {
        1 => RetentionDays.ONE_DAY,
        3 => RetentionDays.THREE_DAYS,
        5 => RetentionDays.FIVE_DAYS,
        7 => RetentionDays.ONE_WEEK,
        14 => RetentionDays.TWO_WEEKS,
        30 => RetentionDays.ONE_MONTH,
        60 => RetentionDays.TWO_MONTHS,
        90 => RetentionDays.THREE_MONTHS,
        180 => RetentionDays.SIX_MONTHS,
        365 => RetentionDays.ONE_YEAR,
        _ => throw new ArgumentOutOfRangeException(nameof(days), days, "Unsupported CloudWatch Logs retention period.")
    };
}
