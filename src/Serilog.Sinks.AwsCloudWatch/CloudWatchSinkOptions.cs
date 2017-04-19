using System;
using Serilog.Events;
using static Serilog.Events.LogEventLevel;

namespace Serilog.Sinks.AwsCloudWatch
{
    /// <summary>
    /// Options that allow configuring the Serilog Sink for AWS CloudWatch
    /// </summary>
    public class CloudWatchSinkOptions
    {
        /// <summary>
        /// The default minimum log event level required in order to write an event to the sink.
        /// </summary>
        public const LogEventLevel DefaultMinimumLogEventLevel = Information;

        /// <summary>
        /// The default batch size to be used when uploading logs to AWS CloudWatch.
        /// </summary>
        public const int DefaultBatchSizeLimit = 100;

        /// <summary>
        /// The default period to be used when a batch upload should be triggered.
        /// </summary>
        public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The minimum log event level required in order to write an event to the sink. Defaults
        /// to <see cref="Information"/>.
        /// </summary>
        public LogEventLevel MinimumLogEventLevel { get; set; } = DefaultMinimumLogEventLevel;

        /// <summary>
        /// The batch size to be used when uploading logs to AWS CloudWatch. Defaults to 100.
        /// </summary>
        public int BatchSizeLimit { get; set; } = DefaultBatchSizeLimit;

        /// <summary>
        /// The period to be used when a batch upload should be triggered. Defaults to 10 seconds.
        /// </summary>
        public TimeSpan Period { get; set; } = DefaultPeriod;

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
