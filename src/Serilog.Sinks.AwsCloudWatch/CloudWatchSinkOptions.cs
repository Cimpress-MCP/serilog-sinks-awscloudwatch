using Serilog.Events;
using Serilog.Formatting;
using System;

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
        public const LogEventLevel DefaultMinimumLogEventLevel = LogEventLevel.Information;

        /// <summary>
        /// The default batch size to be used when uploading logs to AWS CloudWatch.
        /// </summary>
        public const int DefaultBatchSizeLimit = 100;

        /// <summary>
        /// The default to be used when deciding to create the log group or not
        /// </summary>
        public const bool DefaultCreateLogGroup = true;

        /// <summary>
        /// By default, retry an attempt to send log events to CloudWatch Logs 5 times.
        /// </summary>
        public const byte DefaultRetryAttempts = 5;

        /// <summary>
        /// The default period to be used when a batch upload should be triggered.
        /// </summary>
        public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The minimum log event level required in order to write an event to the sink. Defaults
        /// to <see cref="LogEventLevel.Information"/>.
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
        public bool CreateLogGroup { get; set; } = DefaultCreateLogGroup;

        /// <summary>
        /// The log group name to be used in AWS CloudWatch.
        /// </summary>
        public string LogGroupName { get; set; }

        /// <summary>
        /// The respective LogEvent property associated to the groupName value.
        /// </summary>
        public string LogGroupPropertyKey { get; set; }

        /// <summary>
        /// The log stream name to be used in AWS CloudWatch.
        /// </summary>
        public ILogStreamNameProvider LogStreamNameProvider { get; set; } = new DefaultLogStreamProvider();

        /// <summary>
        /// A renderer to render Serilog's LogEvent. It defaults to <see cref="RenderedMessageLogEventRenderer"/>,
        /// which just flattens the log event to a simple string, losing all formatted data.
        /// It's recommended to implement a custom formatter like a simple JSON formatter with various parameters included.
        /// If <see cref="TextFormatter"/> and <see cref="LogEventRenderer"/> are both set then an <see cref="InvalidOperationException"/> will be thrown.
        /// </summary>
        public ILogEventRenderer LogEventRenderer { get; set; }

        /// <summary>
        /// Standard Serilog formatter to convert log events to text instead of the AwsCloudWatch specific <see cref="LogEventRenderer"/>.
        /// If <see cref="TextFormatter"/> and <see cref="LogEventRenderer"/> are both set then an <see cref="InvalidOperationException"/> will be thrown.
        /// </summary>
        public ITextFormatter TextFormatter { get; set; }

        /// <summary>
        /// The number of attempts to retry in the case of a failure.
        /// </summary>
        public byte RetryAttempts { get; set; } = DefaultRetryAttempts;

        /// <summary>
        /// Consider using some incoming event log information to build the log group name.
        /// </summary>
        public bool LogEventBasedLogGroupName { get; set; } = DefaultLogEventBasedLogGroupName;

        /// <summary>
        /// Default value for LogEventBasedLogGroupName.
        /// </summary>
        public const bool DefaultLogEventBasedLogGroupName = false;
    }
}
