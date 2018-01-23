using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly CloudWatchSinkOptions options;
        private bool hasInit;
        private string logStreamName;
        private string nextSequenceToken;
        private readonly ILogEventRenderer renderer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudWatchLogSink"/> class.
        /// </summary>
        /// <param name="cloudWatchClient">The cloud watch client.</param>
        /// <param name="options">The options.</param>
        public CloudWatchLogSink(IAmazonCloudWatchLogs cloudWatchClient, CloudWatchSinkOptions options)
            : base(options.BatchSizeLimit, options.Period)
        {
            if (options.BatchSizeLimit < 1)
            {
                throw new ArgumentException($"{nameof(CloudWatchSinkOptions)}.{nameof(options.BatchSizeLimit)} must be a value greater than 0.");
            }
            this.cloudWatchClient = cloudWatchClient;
            this.options = options;

            if (options.LogEventRenderer != null && options.TextFormatter != null)
            {
                throw new System.InvalidOperationException($"{nameof(options.LogEventRenderer)} and {nameof(options.TextFormatter)} cannot both be applied");
            }

            this.renderer = options.TextFormatter != null
                ? new TextFormatterLogEventRenderer(options.TextFormatter)
                : (options.LogEventRenderer ?? new RenderedMessageLogEventRenderer());
        }

        /// <summary>
        /// Ensures the component is initialized.
        /// </summary>
        private async Task EnsureInitializedAsync(string groupName)
        {
            if (hasInit)
            {
                return;
            }

            // create log group
            await CreateLogGroupAsync(groupName);

            // create log stream
            UpdateLogStreamName();

            await CreateLogStreamAsync(groupName);

            hasInit = true;
        }

        /// <summary>
        /// Creates the log group.
        /// </summary>
        /// <exception cref="Serilog.Sinks.AwsCloudWatch.AwsCloudWatchSinkException"></exception>
        private async Task CreateLogGroupAsync(string groupName)
        {
            if (options.CreateLogGroup)
            {
                // see if the log group already exists
                DescribeLogGroupsRequest describeRequest = new DescribeLogGroupsRequest { LogGroupNamePrefix = groupName };
                var logGroups = await cloudWatchClient.DescribeLogGroupsAsync(describeRequest);
                var logGroup = logGroups.LogGroups.FirstOrDefault(lg => string.Equals(lg.LogGroupName, groupName, StringComparison.OrdinalIgnoreCase));

                // create log group if it doesn't exist
                if (logGroup == null)
                {
                    CreateLogGroupRequest createRequest = new CreateLogGroupRequest(groupName);
                    var createResponse = await cloudWatchClient.CreateLogGroupAsync(createRequest);
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
        /// Creates the log stream.
        /// </summary>
        /// <exception cref="Serilog.Sinks.AwsCloudWatch.AwsCloudWatchSinkException"></exception>
        private async Task CreateLogStreamAsync(string groupName)
        {
            CreateLogStreamRequest createLogStreamRequest = new CreateLogStreamRequest()
            {
                LogGroupName = groupName,
                LogStreamName = logStreamName
            };
            var createLogStreamResponse = await cloudWatchClient.CreateLogStreamAsync(createLogStreamRequest);
        }

        /// <summary>
        /// Updates the log stream sequence token.
        /// </summary>
        /// <exception cref="Serilog.Sinks.AwsCloudWatch.AwsCloudWatchSinkException"></exception>
        private async Task UpdateLogStreamSequenceTokenAsync()
        {
            DescribeLogStreamsRequest describeLogStreamsRequest = new DescribeLogStreamsRequest
            {
                LogGroupName = options.LogGroupName,
                LogStreamNamePrefix = logStreamName
            };
            var describeLogStreamsResponse = await cloudWatchClient.DescribeLogStreamsAsync(describeLogStreamsRequest);
            nextSequenceToken = describeLogStreamsResponse.NextToken;
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
        /// <param name="groupName">The group associated to the batch.</param>
        private async Task PublishBatchAsync(PutLogEventsRequest putLogEventsRequest)
        {
            if (putLogEventsRequest == null)
                return;

            if (putLogEventsRequest.LogEvents?.Count == 0)
            {
                return;
            }

            var success = false;
            var attemptIndex = 0;
            while (!success && attemptIndex <= options.RetryAttempts)
            {
                try
                {
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
                    await CreateLogGroupAsync(putLogEventsRequest.LogGroupName);
                    await CreateLogStreamAsync(putLogEventsRequest.LogGroupName);
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
                        await CreateLogStreamAsync(putLogEventsRequest.LogGroupName);
                        putLogEventsRequest.LogStreamName = logStreamName;
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
                        await CreateLogStreamAsync(putLogEventsRequest.LogGroupName);
                        putLogEventsRequest.LogStreamName = logStreamName;
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
        /// Creates a PutLogEventsRequest to send events to CloudWatch.
        /// </summary>
        /// <param name="groupName">Respective log group name for this batch.</param>
        /// <param name="batch">Batch of log events.</param>
        /// <returns></returns>
        private PutLogEventsRequest CreateLogEventsRequest(string groupName, List<InputLogEvent> batch)
        {
            // creates the request to upload a new event to CloudWatch
            return new PutLogEventsRequest
            {
                LogGroupName = groupName,
                LogStreamName = logStreamName,
                SequenceToken = nextSequenceToken,
                LogEvents = batch
            };
        }

        /// <summary>
        /// Transforms Serilog's LogEvent into AWS CloudWatch InputLogEvent.
        /// </summary>
        /// <param name="event">The event itself.</param>
        /// <returns></returns>
        private InputLogEvent TransformToInputLogEvent(LogEvent @event)
        {
            var message = renderer.RenderLogEvent(@event);
            var messageLength = System.Text.Encoding.UTF8.GetByteCount(message);
            if (messageLength > MaxLogEventSize)
            {
                // truncate event message
                Debugging.SelfLog.WriteLine("Truncating log event with length of {0}",
                    messageLength);
                message = message.Substring(0, MaxLogEventSize);
            }
            return new InputLogEvent
            {
                Message = message,
                Timestamp = @event.Timestamp.UtcDateTime
            };
        }

        /// <summary>
        /// Responsible for the entire processing of events on it's own log group context, defined by the groupName.
        /// </summary>
        /// <param name="events"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
        private async Task CreateAndProcessLogEvents(IEnumerable<LogEvent> events, string groupName)
        {
            var logEvents = new Queue<InputLogEvent>(events
                .OrderBy(e => e.Timestamp)
                .Select(TransformToInputLogEvent));

            try
            {
                await EnsureInitializedAsync(groupName);
            }
            catch (Exception ex)
            {
                Debugging.SelfLog.WriteLine("Error initializing log stream. No logs will be sent to AWS CloudWatch. Exception was {0}.", ex);
                return;
            }

            while (logEvents.Count > 0)
            {
                var batch = CreateBatch(logEvents);

                var putLogEventsRequest = CreateLogEventsRequest(groupName, batch);

                await PublishBatchAsync(putLogEventsRequest);
            }
        }

        /// <summary>
        /// Emit a batch of log events, running asynchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            // We do not need synchronization in this method since it is only called from a single thread by the PeriodicBatchSink.

            if (events?.Count() == 0)
            {
                return;
            }

            try
            {
                if (options.LogEventBasedLogGroupName)
                {
                    var logGroups = new Dictionary<string, List<LogEvent>>();

                    foreach (var e in events)
                    {
                        var logGroupName = e.Properties[options.LogGroupPropertyKey].ToString().Replace("\"", "");

                        if (logGroups.ContainsKey(logGroupName))
                            logGroups[logGroupName].Add(e);
                        else
                        {
                            logGroups.Add(logGroupName, new List<LogEvent>(){e});
                        }
                    }

                    foreach (var group in logGroups)
                    {
                        await CreateAndProcessLogEvents(group.Value, group.Key);
                    }
                }
                else
                {
                    await CreateAndProcessLogEvents(events, options.LogGroupName);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Debugging.SelfLog.WriteLine("Error sending logs. No logs will be sent to AWS CloudWatch. Error was {0}", ex);
                }
                catch (Exception)
                {
                    // we even failed to log to the trace logger - giving up trying to put something out
                }
            }
        }
    }
}
