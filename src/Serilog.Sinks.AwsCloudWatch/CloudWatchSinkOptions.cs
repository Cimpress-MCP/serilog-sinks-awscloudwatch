using Serilog.Events;
using Serilog.Formatting;
using System;

namespace Serilog.Sinks.AwsCloudWatch
{
    /// <summary>
    /// Options that allow configuring the Serilog Sink for AWS CloudWatch
    /// </summary>
    public class CloudWatchSinkOptions : ICloudWatchSinkOptions
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
        /// The default qeueue size to be used creating the queue used to hold batched log events.
        /// Defaults to a very large number to retain backwards compatibitliy for old clients who
        /// relied on unbounded queue sizes while still providing safe runtime behavior.
        /// It is recommended to tune this queue size along with batchSize for your application logging use cases.
        /// </summary>
        public const int DefaultQueueSizeLimit = 10000;

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
        /// The queue size to be used when holding batched log events in memory. Defaults to 10000.
        /// </summary>
        public int QueueSizeLimit { get; set; } = DefaultQueueSizeLimit;

        /// <summary>
        /// The period to be used when a batch upload should be triggered. Defaults to 10 seconds.
        /// </summary>
        public TimeSpan Period { get; set; } = DefaultPeriod;

        /// <summary>
        /// The number of days to retain the log events in the specified log group.
        /// </summary>
        public LogGroupRetentionPolicy LogGroupRetentionPolicy { get; set; } = LogGroupRetentionPolicy.Indefinitely;

        /// <summary>
        /// The log group name to be used in AWS CloudWatch.
        /// </summary>
        public bool CreateLogGroup { get; set; } = DefaultCreateLogGroup;

        /// <summary>
        /// The log group name to be used in AWS CloudWatch.
        /// </summary>
        public string LogGroupName { get; set; }

        /// <summary>
        /// The log stream name to be used in AWS CloudWatch.
        /// </summary>
        public ILogStreamNameProvider LogStreamNameProvider { get; set; } = new DefaultLogStreamProvider();


        /// <summary>
        /// Standard Serilog formatter to convert log events to text.
        /// </summary>
        public ITextFormatter TextFormatter { get; set; }

        /// <summary>
        /// The number of attempts to retry in the case of a failure.
        /// </summary>
        public byte RetryAttempts { get; set; } = DefaultRetryAttempts;
    }
}
