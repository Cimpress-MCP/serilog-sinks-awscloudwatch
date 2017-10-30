using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Serilog.Sinks.AwsCloudWatch
{
    public class CloudWatchLogSink : PeriodicBatchingSink
    {
        /// <summary>
        /// The maximum log event size = 256 KB - 26 bytes
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
        /// The maximum span of events in a batch
        /// </summary>
        public static readonly TimeSpan MaxBatchEventSpan = TimeSpan.FromDays(1);

        /// <summary>
        /// The span of time to throttle requests at
        /// </summary>
        public static readonly TimeSpan ThrottlingInterval = TimeSpan.FromMilliseconds(200);

        private readonly IAmazonCloudWatchLogs cloudWatchClient;
        private readonly CloudWatchSinkOptions options;
        private bool hasInit;
        private string logStreamName;
        private string nextSequenceToken;
        private readonly ILogEventRenderer renderer;

        public CloudWatchLogSink(IAmazonCloudWatchLogs cloudWatchClient, CloudWatchSinkOptions options)
            : base(options.BatchSizeLimit, options.Period)
        {
            if (options.BatchSizeLimit < 1)
            {
                throw new ArgumentException($"{nameof(CloudWatchSinkOptions)}.{nameof(options.BatchSizeLimit)} must be a value greater than 0.");
            }
            this.cloudWatchClient = cloudWatchClient;
            this.options = options;
            this.renderer = options.LogEventRenderer ?? new RenderedMessageLogEventRenderer();
        }

        private async Task EnsureInitialized()
        {
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            if (events.Count() == 0)
            {
                return;
            }

            // We do not need synchronization in this method since it is only called from a single thread by the PeriodicBatchSink.

            try
            {
                await EnsureInitialized();
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
                                var message = renderer.RenderLogEvent(@event);
                                if (message.Length > MaxLogEventSize)
                                {
                                    Debugging.SelfLog.WriteLine("Truncating log event with length of {0}", message.Length);
                                    message = message.Substring(0, MaxLogEventSize);
                                }
                                return new InputLogEvent
                                {
                                    Message = message,
                                    Timestamp = @event.Timestamp.UtcDateTime
                                };
                            }));

                while (logEvents.Count > 0)
                {
                    DateTime? first = null;
                    var batchSize = 0;
                    var batch = new List<InputLogEvent>();

                    do
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

                        if (batchSize + @event.Message.Length < MaxLogEventBatchSize) // ensure < max batch size
                        {
                            batchSize += @event.Message.Length;
                            batch.Add(@event);
                            logEvents.Dequeue();
                        }
                        else
                        {
                            break;
                        }
                    } while (batch.Count < MaxLogEventBatchCount && logEvents.Count > 0); // ensure < max batch count

                    // creates the request to upload a new event to CloudWatch
                    PutLogEventsRequest putEventsRequest = new PutLogEventsRequest
                    {
                        LogGroupName = options.LogGroupName,
                        LogStreamName = logStreamName,
                        SequenceToken = nextSequenceToken,
                        LogEvents = batch
                    };

                    // actually upload the event to CloudWatch
                    var putLogEventsResponse = await cloudWatchClient.PutLogEventsAsync(putEventsRequest);

                    // remember the next sequence token, which is required
                    nextSequenceToken = putLogEventsResponse.NextSequenceToken;

                    // throttle
                    await Task.Delay(ThrottlingInterval);
                }


                //logEvents.TakeWhile(@event =>
                //{
                //    return true;
                //});


                //var attempt = 0;
                //do
                //{

                //} while (attempt < options.RetryAttempts);

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
