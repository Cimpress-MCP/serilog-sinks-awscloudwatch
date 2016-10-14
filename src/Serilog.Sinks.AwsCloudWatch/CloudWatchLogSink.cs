using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using System.Linq;
using Serilog.Core;
using Serilog.Formatting.Display;

namespace Serilog.Sinks.AwsCloudWatch
{
    internal class CloudWatchLogSink : PeriodicBatchingSink
    {
        const string DefaultOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}";

        private readonly IAmazonCloudWatchLogs cloudWatchClient;
        private readonly CloudWatchSinkOptions options;
        private bool hasInit;
        private string logStreamName;
        private string nextSequenceToken;
        [Obsolete("Use textFormatter instead.")]
        private readonly ILogEventRenderer renderer;
        private readonly Serilog.Formatting.ITextFormatter textFormatter;
        private readonly Logger traceLogger;

        public CloudWatchLogSink(IAmazonCloudWatchLogs cloudWatchClient, CloudWatchSinkOptions options) : base(options.BatchSizeLimit, options.Period)
        {
            this.cloudWatchClient = cloudWatchClient;
            this.options = options;

            if (options.LogEventRenderer != null) renderer = options.LogEventRenderer;
            textFormatter = options.TextFormatter ?? new MessageTemplateTextFormatter(DefaultOutputTemplate, options.FormatProvider);

            UpdateLogStreamName();

            traceLogger = new LoggerConfiguration().WriteTo.Trace().CreateLogger();
        }

        /// <summary>
        /// Creates a new log stream name, based on current time and a unique identifier.
        /// Is uses a sortable, but simplified date format that conforms with naming requirements of the log stream naming,
        /// but allows to the log stream to be sorted and easily identified.
        /// </summary>
        private void UpdateLogStreamName()
        {
            var prefix = DateTime.UtcNow.ToString("yyyy-MM-dd-hh-mm-ss");
            logStreamName = $"{prefix}_{Dns.GetHostName()}_{Guid.NewGuid()}";
            hasInit = false;
        }

        private async Task EnsureInitialized()
        {
            if (hasInit)
            {
                return;
            }

            // see if the log group already exists
            DescribeLogGroupsRequest describeRequest = new DescribeLogGroupsRequest {LogGroupNamePrefix = options.LogGroupName};
            var logGroups = await cloudWatchClient.DescribeLogGroupsAsync(describeRequest);
            var logGroup = logGroups.LogGroups.FirstOrDefault(lg => string.Equals(lg.LogGroupName, options.LogGroupName, StringComparison.OrdinalIgnoreCase));

            // create log group if it doesn't exist
            if (logGroup == null)
            {
                CreateLogGroupRequest createRequest = new CreateLogGroupRequest(options.LogGroupName);
                var createResponse = await cloudWatchClient.CreateLogGroupAsync(createRequest);
                if (!createResponse.HttpStatusCode.IsSuccessStatusCode())
                {
                    throw new Exception($"Tried to create a log group, but failed with status code '{createResponse.HttpStatusCode}' and data '{createResponse.ResponseMetadata.FlattenedMetaData()}'.");
                }
            }

            // create log stream
            CreateLogStreamRequest createLogStreamRequest = new CreateLogStreamRequest()
            {
                LogGroupName = options.LogGroupName,
                LogStreamName = logStreamName
            };
            var createLogStreamResponse = await cloudWatchClient.CreateLogStreamAsync(createLogStreamRequest);
            if (!createLogStreamResponse.HttpStatusCode.IsSuccessStatusCode())
            {
                throw new Exception(
                    $"Tried to create a log stream, but failed with status code '{createLogStreamResponse.HttpStatusCode}' and data '{createLogStreamResponse.ResponseMetadata.FlattenedMetaData()}'.");
            }

            hasInit = true;
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
                Console.WriteLine("Error initializing log stream. No logs will be sent to AWS CloudWatch.");
                Console.WriteLine(ex);
                return;
            }
            
            try
            {
                // log events need to be ordered by timestamp within a single bulk upload to CloudWatch
                var logEvents =
                    events.OrderBy(e => e.Timestamp)
                        .Select(e =>
                        {
                            // use the textFormatter to build formatted output
                            string message = "";

                            if (renderer != null)
                            {
                                message = renderer.RenderLogEvent(e);
                            }
                            else
                            {
                                using (var writer = new System.IO.StringWriter())
                                {
                                    textFormatter.Format(e, writer);
                                    writer.Flush();
                                    message = writer.ToString();
                                }
                            }

                            return new InputLogEvent {Message = message, Timestamp = e.Timestamp.UtcDateTime};
                        })
                        .ToList();

                // creates the request to upload a new event to CloudWatch
                PutLogEventsRequest putEventsRequest = new PutLogEventsRequest(options.LogGroupName, logStreamName, logEvents)
                {
                    SequenceToken = nextSequenceToken
                };

                // actually upload the event to CloudWatch
                var putLogEventsResponse = await cloudWatchClient.PutLogEventsAsync(putEventsRequest);

                // validate success
                if (!putLogEventsResponse.HttpStatusCode.IsSuccessStatusCode())
                {
                    // let's start a new log stream in case anything went wrong with the upload
                    UpdateLogStreamName();
                    throw new Exception(
                        $"Tried to send logs, but failed with status code '{putLogEventsResponse.HttpStatusCode}' and data '{putLogEventsResponse.ResponseMetadata.FlattenedMetaData()}'.");
                }

                // remember the next sequence token, which is required
                nextSequenceToken = putLogEventsResponse.NextSequenceToken;
            }
            catch (Exception ex)
            {
                try
                {
                    traceLogger.Error("Error sending logs. No logs will be sent to AWS CloudWatch. Error was {0}", ex);
                }
                catch (Exception)
                {
                    // we event failed to log to the trace logger - giving up trying to put something out
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                cloudWatchClient.Dispose();
            }
        }
    }
}