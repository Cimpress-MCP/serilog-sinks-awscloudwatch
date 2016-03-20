using System;

namespace Serilog.Sinks.AwsCloudWatch
{
    /// <summary>
    /// Options that allow configuring the Serilog Sink for AWS CloudWatch 
    /// </summary>
    public class CloudWatchSinkOptions
    {
        /// <summary>
        /// The batch size to be used when uploading logs to cloud watch. Defaults to 100.
        /// </summary>
        public int BatchSizeLimit { get; set; } = 100;

        /// <summary>
        /// The period to be used when a batch upload should be triggered. Defaults to 10 seconds.
        /// </summary>
        public TimeSpan Period { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The log group name to be used in AWS CloudWatch.
        /// </summary>
        public string LogGroupName { get; set; }

        /// <summary>
        /// A renderer to render Serilog's LogEvent. It defaults to <see cref="RenderedMessageLogEventRenderer"/>,
        /// which just flattens the log event to a simple string, losing all formatted data.
        /// It's recommended to implement a custom formatter like a simple JSON formatter with various parameters included.
        /// </summary>
        public ILogEventRenderer LogEventRenderer { get; set; }
    }
}