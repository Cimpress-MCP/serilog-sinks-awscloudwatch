using System;

namespace Serilog.Sinks.AwsCloudWatch
{
    public class CloudWatchSinkOptions
    {
        public int BatchSizeLimit { get; set; } = 100;
        public TimeSpan Period { get; set; } = TimeSpan.FromSeconds(10);
        public string LogGroupName { get; set; }
    }
}