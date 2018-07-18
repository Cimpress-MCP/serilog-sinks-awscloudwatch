using Serilog.Events;
using Serilog.Formatting;
using System;

namespace Serilog.Sinks.AwsCloudWatch
{
    /// <summary>
    /// The number of days to retain the log events in the specified log group.
    /// <see href="https://docs.aws.amazon.com/AmazonCloudWatchLogs/latest/APIReference/API_PutRetentionPolicy.html"/>
    /// </summary>
    public enum LogGroupRetentionPolicy
    {
        Indefinitely = 0,
        OneDay = 1,
        ThreeDays = 3,
        FiveDays = 5,
        OneWeek = 7,
        TwoWeeks = 14,
        OneMonth = 30,
        TwoMonths = 60,
        ThreeMonths = 90,
        FourMonths = 120,
        FiveMonths = 150,
        SixMonths = 180,
        OneYear = 365,
        OneYearAndOneMonth = 400,
        OneYearAndSixMonths = 545,
        TwoYears = 731,
        FiveYears = 1827,
        TenYears = 3653
    }
}
