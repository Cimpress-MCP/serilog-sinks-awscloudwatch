using Amazon.CloudWatchLogs;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.Threading.Tasks;
using Serilog.Core;

namespace Serilog.Sinks.AwsCloudWatch
{
    /// <summary>
    /// A Serilog log sink that publishes to AWS CloudWatch Logs
    /// </summary>
    public class CloudWatchLogSink : ILogEventSink, IDisposable, IAsyncDisposable
    {
        private readonly PeriodicBatchingSink batchingSink;

        /// <summary>
        /// The maximum log event size = 256 KB - 26 B
        /// </summary>
        public const int MaxLogEventSize = 262118;

        /// <summary>
        /// The maximum log event batch size = 1 MB
        /// </summary>
        public const int MaxLogEventBatchSize = 1048576;

        /// <summary>
        /// The maximum log event batch count
        /// </summary>
        public const int MaxLogEventBatchCount = 10000;

        /// <summary>
        /// When in a batch, each message must have a buffer of 26 bytes
        /// </summary>
        public const int MessageBufferSize = 26;

        /// <summary>
        /// The maximum span of events in a batch
        /// </summary>
        public static readonly TimeSpan MaxBatchEventSpan = TimeSpan.FromDays(1);

        /// <summary>
        /// The span of time to backoff when an error occurs
        /// </summary>
        public static readonly TimeSpan ErrorBackoffStartingInterval = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudWatchLogSink"/> class.
        /// </summary>
        /// <param name="cloudWatchClient">The cloud watch client.</param>
        /// <param name="options">The options.</param>
        public CloudWatchLogSink(IAmazonCloudWatchLogs cloudWatchClient, ICloudWatchSinkOptions options)
        {
            var batchedSink = new CloudWatchLogsBatchedSink(cloudWatchClient, options);
            batchingSink = new(batchedSink, new() { BatchSizeLimit = options.BatchSizeLimit, Period = options.Period, QueueLimit = options.QueueSizeLimit });
        }
        
        /// <inheritdoc/>
        public void Emit(LogEvent logEvent)
        {
            batchingSink.Emit(logEvent);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            batchingSink.Dispose();
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            return batchingSink.DisposeAsync();
        }
    }
}