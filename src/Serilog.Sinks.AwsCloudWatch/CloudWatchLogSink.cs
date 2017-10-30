using Amazon.CloudWatchLogs;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog.Events;
using Amazon.CloudWatchLogs.Model;

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
                // log events need to be ordered by timestamp within a single bulk upload to CloudWatch
                var logEvents =
                    events.OrderBy(e => e.Timestamp)
                        .Select(e =>
                        {
                            var message = renderer.RenderLogEvent(e);
                            return new InputLogEvent
                            {
                                Message = message.Substring(0, Math.Min(message.Length, MaxLogEventSize)),
                                //Message = message,
                                Timestamp = e.Timestamp.UtcDateTime
                            };
                        })
                        .ToList();
                //.GroupBy(item => item.Timestamp.Date); // ensures a batch will not span more than 24 hours

                //logEvents.TakeWhile(@event =>
                //{
                //    return true;
                //});


                //var attempt = 0;
                //do
                //{

                //} while (attempt < options.RetryAttempts);

                // creates the request to upload a new event to CloudWatch
                PutLogEventsRequest putEventsRequest = new PutLogEventsRequest(options.LogGroupName, logStreamName, logEvents)
                {
                    SequenceToken = nextSequenceToken
                };

                // actually upload the event to CloudWatch
                var putLogEventsResponse = await cloudWatchClient.PutLogEventsAsync(putEventsRequest);
            }
            catch (Exception ex)
            {
                Console.WriteLine("bad, bad, bad... {0}", ex);
            }
        }
    }
}
