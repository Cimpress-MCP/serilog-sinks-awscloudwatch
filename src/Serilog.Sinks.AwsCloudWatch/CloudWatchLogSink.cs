using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Serilog.Sinks.AwsCloudWatch
{
    /// <summary>
    /// A Serilog log sink that publishes to AWS CloudWatch Logs
    /// </summary>
    /// <seealso cref="Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink" />
    public class CloudWatchLogSink : PeriodicBatchingSink
    {
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

        private readonly IAmazonCloudWatchLogs cloudWatchClient;
        private readonly ICloudWatchSinkOptions options;
        private bool hasInit;
        private string logStreamName;
        private string nextSequenceToken;
        private readonly ITextFormatter textFormatter;

        private readonly SemaphoreSlim syncObject = new SemaphoreSlim(1);

#pragma warning disable CS0618
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudWatchLogSink"/> class.
        /// </summary>
        /// <param name="cloudWatchClient">The cloud watch client.</param>
        /// <param name="options">The options.</param>
        public CloudWatchLogSink(IAmazonCloudWatchLogs cloudWatchClient, ICloudWatchSinkOptions options) : base(options.BatchSizeLimit, options.Period, options.QueueSizeLimit)
        {
            if (string.IsNullOrEmpty(options?.LogGroupName))
            {
                throw new ArgumentException($"{nameof(ICloudWatchSinkOptions)}.{nameof(options.LogGroupName)} must be specified.");
            }
            if (options.BatchSizeLimit < 1)
            {
                throw new ArgumentException($"{nameof(ICloudWatchSinkOptions)}.{nameof(options.BatchSizeLimit)} must be a value greater than 0.");
            }
            this.cloudWatchClient = cloudWatchClient;
            this.options = options;

            if (options.TextFormatter == null)
            {
                throw new System.ArgumentException($"{nameof(options.TextFormatter)} is required");
            }

            textFormatter = options.TextFormatter;
        }
#pragma warning restore CS0618

        /// <summary>
        /// Ensures the component is initialized.
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (hasInit)
            {
                return;
            }

            // create log group
            await CreateLogGroupAsync();

            // create log stream
            UpdateLogStreamName();
            await CreateLogStreamAsync();

            hasInit = true;
        }

        /// <summary>
        /// Creates the log group.
        /// </summary>
        private async Task CreateLogGroupAsync()
        {
            if (options.CreateLogGroup)
            {
                // see if the log group already exists
                var describeRequest = new DescribeLogGroupsRequest
                {
                    LogGroupNamePrefix = options.LogGroupName
                };

                var logGroups = await cloudWatchClient
                    .DescribeLogGroupsAsync(describeRequest);

                var logGroup = logGroups
                    .LogGroups
                    .FirstOrDefault(lg => string.Equals(lg.LogGroupName, options.LogGroupName, StringComparison.Ordinal));

                // create log group if it doesn't exist
                if (logGroup == null)
                {
                    var createRequest = new CreateLogGroupRequest(options.LogGroupName);
                    var createResponse = await cloudWatchClient.CreateLogGroupAsync(createRequest);

                    // update the retention policy if a specific period is defined
                    if (options.LogGroupRetentionPolicy != LogGroupRetentionPolicy.Indefinitely)
                    {
                        var putRetentionRequest = new PutRetentionPolicyRequest(options.LogGroupName, (int)options.LogGroupRetentionPolicy);
                        await cloudWatchClient.PutRetentionPolicyAsync(putRetentionRequest);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the name of the log stream.
        /// </summary>
        private void UpdateLogStreamName()
        {
            logStreamName = options.LogStreamNameProvider.GetLogStreamName();
            nextSequenceToken = null; // always reset on a new stream
        }

        /// <summary>
        /// Creates the log stream if needed.
        /// </summary>
        private async Task CreateLogStreamAsync()
        {
            // see if the log stream already exists
            var logStream = await GetLogStreamAsync();

            // create log stream if it doesn't exist
            if (logStream == null)
            {
                var createLogStreamRequest = new CreateLogStreamRequest
                {
                    LogGroupName = options.LogGroupName,
                    LogStreamName = logStreamName
                };
                var createLogStreamResponse = await cloudWatchClient.CreateLogStreamAsync(createLogStreamRequest);
            }
            else
            {
                nextSequenceToken = logStream.UploadSequenceToken;
            }
        }

        /// <summary>
        /// Updates the log stream sequence token.
        /// </summary>
        private async Task UpdateLogStreamSequenceTokenAsync()
        {
            var logStream = await GetLogStreamAsync();
            nextSequenceToken = logStream?.UploadSequenceToken;
        }

        /// <summary>
        /// Attempts to get the log stream defined by <see cref="logStreamName"/>.
        /// </summary>
        /// <returns>The matching log stream or null if no match can be found.</returns>
        private async Task<LogStream> GetLogStreamAsync()
        {
            var describeLogStreamsRequest = new DescribeLogStreamsRequest
            {
                LogGroupName = options.LogGroupName,
                LogStreamNamePrefix = logStreamName
            };

            var describeLogStreamsResponse = await cloudWatchClient
                .DescribeLogStreamsAsync(describeLogStreamsRequest);

            return describeLogStreamsResponse
                .LogStreams
                .SingleOrDefault(ls => string.Equals(ls.LogStreamName, logStreamName, StringComparison.Ordinal));
        }

        /// <summary>
        /// Creates a batch of events.
        /// </summary>
        /// <param name="logEvents">The entire set of log events.</param>
        /// <returns>A batch of events meeting defined restrictions.</returns>
        private List<InputLogEvent> CreateBatch(Queue<InputLogEvent> logEvents)
        {
            DateTime? first = null;
            var batchSize = 0;
            var batch = new List<InputLogEvent>();

            while (batch.Count < MaxLogEventBatchCount && logEvents.Count > 0) // ensure < max batch count
            {
                var @event = logEvents.Peek();

                if (!first.HasValue)
                {
                    first = @event.Timestamp;
                }
                else if (@event.Timestamp.Subtract(first.Value) > MaxBatchEventSpan) // ensure batch spans no more than 24 hours
                {
                    break;
                }

                var proposedBatchSize = batchSize + System.Text.Encoding.UTF8.GetByteCount(@event.Message) + MessageBufferSize;
                if (proposedBatchSize < MaxLogEventBatchSize) // ensure < max batch size
                {
                    batchSize = proposedBatchSize;
                    batch.Add(@event);
                    logEvents.Dequeue();
                }
                else
                {
                    break;
                }
            }

            return batch;
        }

        /// <summary>
        /// Publish the batch of log events to AWS CloudWatch Logs.
        /// </summary>
        /// <param name="batch">The request.</param>
        private async Task PublishBatchAsync(List<InputLogEvent> batch)
        {
            if (batch?.Count == 0)
            {
                return;
            }

            var success = false;
            var attemptIndex = 0;
            while (!success && attemptIndex <= options.RetryAttempts)
            {
                try
                {
                    // creates the request to upload a new event to CloudWatch
                    var putLogEventsRequest = new PutLogEventsRequest
                    {
                        LogGroupName = options.LogGroupName,
                        LogStreamName = logStreamName,
                        SequenceToken = nextSequenceToken,
                        LogEvents = batch
                    };

                    // actually upload the event to CloudWatch
                    var putLogEventsResponse = await cloudWatchClient.PutLogEventsAsync(putLogEventsRequest);

                    // remember the next sequence token, which is required
                    nextSequenceToken = putLogEventsResponse.NextSequenceToken;

                    success = true;
                }
                catch (ServiceUnavailableException e)
                {
                    // retry with back-off
                    Debugging.SelfLog.WriteLine("Service unavailable.  Attempt: {0}  Error: {1}", attemptIndex, e);
                    await Task.Delay(ErrorBackoffStartingInterval.Milliseconds * (int)Math.Pow(2, attemptIndex));
                    attemptIndex++;
                }
                catch (ResourceNotFoundException e)
                {
                    // no retry with back-off because..
                    //   if one of these fails, we get out of the loop.
                    //   if they're both successful, we don't hit this case again.
                    Debugging.SelfLog.WriteLine("Resource was not found.  Error: {0}", e);
                    await CreateLogGroupAsync();
                    await CreateLogStreamAsync();
                }
                catch (DataAlreadyAcceptedException e)
                {
                    Debugging.SelfLog.WriteLine("Data already accepted.  Attempt: {0}  Error: {1}", attemptIndex, e);
                    try
                    {
                        await UpdateLogStreamSequenceTokenAsync();
                    }
                    catch (Exception ex)
                    {
                        Debugging.SelfLog.WriteLine("Unable to update log stream sequence.  Attempt: {0}  Error: {1}", attemptIndex, ex);

                        // try again with a different log stream
                        UpdateLogStreamName();
                        await CreateLogStreamAsync();
                    }
                    attemptIndex++;
                }
                catch (InvalidSequenceTokenException e)
                {
                    Debugging.SelfLog.WriteLine("Invalid sequence token.  Attempt: {0}  Error: {1}", attemptIndex, e);
                    try
                    {
                        await UpdateLogStreamSequenceTokenAsync();
                    }
                    catch (Exception ex)
                    {
                        Debugging.SelfLog.WriteLine("Unable to update log stream sequence.  Attempt: {0}  Error: {1}", attemptIndex, ex);

                        // try again with a different log stream
                        UpdateLogStreamName();
                        await CreateLogStreamAsync();
                    }
                    attemptIndex++;
                }
                catch (Exception e)
                {
                    Debugging.SelfLog.WriteLine("Unhandled exception.  Error: {0}", e);
                    break;
                }
            }
        }

        /// <summary>
        /// Emit a batch of log events, running asynchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            try
            {
                await syncObject.WaitAsync();

                if (events?.Count() == 0)
                {
                    return;
                }

                try
                {
                    await EnsureInitializedAsync();
                }
                catch (Exception ex)
                {
                    Debugging.SelfLog.WriteLine("Error initializing log stream. No logs will be sent to AWS CloudWatch. Exception was {0}.", ex);
                    return;
                }

                try
                {
                    var logEvents =
                        new Queue<InputLogEvent>(events
                            .OrderBy(e => e.Timestamp) // log events need to be ordered by timestamp within a single bulk upload to CloudWatch
                            .Select( // transform
                                @event =>
                                {
                                    string message = null;
                                    using (var writer = new StringWriter())
                                    {
                                        textFormatter.Format(@event, writer);
                                        writer.Flush();
                                        message = writer.ToString();
                                    }
                                    var messageLength = Encoding.UTF8.GetByteCount(message);
                                    if (messageLength > MaxLogEventSize)
                                    {
                                        // truncate event message
                                        Debugging.SelfLog.WriteLine("Truncating log event with length of {0}", messageLength);
                                        var buffer = Encoding.UTF8.GetBytes(message);
                                        message = Encoding.UTF8.GetString(buffer, 0, MaxLogEventSize);
                                    }
                                    return new InputLogEvent
                                    {
                                        Message = message,
                                        Timestamp = @event.Timestamp.UtcDateTime
                                    };
                                }));

                    while (logEvents.Count > 0)
                    {
                        var batch = CreateBatch(logEvents);

                        await PublishBatchAsync(batch);
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        Debugging.SelfLog.WriteLine("Error sending logs. No logs will be sent to AWS CloudWatch. Error was {0}", ex);
                    }
                    catch
                    {
                        // we even failed to log to the trace logger - giving up trying to put something out
                    }
                }
            }
            finally
            {
                syncObject.Release();
            }
        }
    }
}