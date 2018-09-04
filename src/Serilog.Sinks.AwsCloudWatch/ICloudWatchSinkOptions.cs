using Serilog.Events;
using Serilog.Formatting;
using System;

namespace Serilog.Sinks.AwsCloudWatch
{
    /// <summary>
    /// Interface which allows runtime construction of the options via appsettings configuration.
    /// </summary>
    public interface ICloudWatchSinkOptions
    {
        /// <summary>
        /// The minimum log event level required in order to write an event to the sink. Defaults
        /// to <see cref="LogEventLevel.Information"/>.
        /// </summary>
        LogEventLevel MinimumLogEventLevel { get; }

        /// <summary>
        /// The batch size to be used when uploading logs to AWS CloudWatch. Defaults to 100.
        /// </summary>
        int BatchSizeLimit { get; }

        /// <summary>
        /// The queue size to be used when holding batched log events in memory. Defaults to 10000.
        /// </summary>
        int QueueSizeLimit { get; }

        /// <summary>
        /// The period to be used when a batch upload should be triggered. Defaults to 10 seconds.
        /// </summary>
        TimeSpan Period { get; }

        /// <summary>
        /// The number of days to retain the log events in the specified log group.
        /// </summary>
        LogGroupRetentionPolicy LogGroupRetentionPolicy { get; set; }

        /// <summary>
        /// The log group name to be used in AWS CloudWatch.
        /// </summary>
        bool CreateLogGroup { get; }

        /// <summary>
        /// The log group name to be used in AWS CloudWatch.
        /// </summary>
        string LogGroupName { get; }

        /// <summary>
        /// The log stream name to be used in AWS CloudWatch.
        /// </summary>
        ILogStreamNameProvider LogStreamNameProvider { get; }


        /// <summary>
        /// Standard Serilog formatter to convert log events to text.
        /// </summary>
        ITextFormatter TextFormatter { get; }

        /// <summary>
        /// The number of attempts to retry in the case of a failure.
        /// </summary>
        byte RetryAttempts { get; }
    }
}
