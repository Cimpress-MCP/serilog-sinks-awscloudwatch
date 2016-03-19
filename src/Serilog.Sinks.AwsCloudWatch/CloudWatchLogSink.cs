using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Newtonsoft.Json;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using System.Linq;

namespace Serilog.Sinks.AwsCloudWatch
{
    internal class CloudWatchLogSink : PeriodicBatchingSink
    {
        private readonly IAmazonCloudWatchLogs cloudWatchClient;
        private readonly CloudWatchSinkOptions options;
        private bool hasInit;
        private readonly string logStreamName;
        private string nextSequenceToken;

        public CloudWatchLogSink(IAmazonCloudWatchLogs cloudWatchClient, CloudWatchSinkOptions options) : base(options.BatchSizeLimit, options.Period)
        {
            this.cloudWatchClient = cloudWatchClient;
            this.options = options;
            hasInit = false;

            var prefix = DateTime.UtcNow.ToString("yyyy-MM-dd-hh-mm-ss");
            logStreamName = $"{prefix}_{Dns.GetHostName()}_{Guid.NewGuid()}";
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
                var logEvents = events.OrderBy(e => e.Timestamp).Select(e =>
                {
                    var writer = new StringWriter();
                    e.RenderMessage(writer);
                    var message = new {e.MessageTemplate, e.Properties, RenderedMessage = writer.ToString(), e.Level, e.Exception};
                    return new InputLogEvent {Message = JsonConvert.SerializeObject(message), Timestamp = e.Timestamp.UtcDateTime};
                }).ToList();
                PutLogEventsRequest putEventsRequest = new PutLogEventsRequest(options.LogGroupName, logStreamName, logEvents)
                {
                    SequenceToken = nextSequenceToken
                };
                var putLogEventsResponse = await cloudWatchClient.PutLogEventsAsync(putEventsRequest);
                if (!putLogEventsResponse.HttpStatusCode.IsSuccessStatusCode())
                {
                    throw new Exception(
                        $"Tried to send logs, but failed with status code '{putLogEventsResponse.HttpStatusCode}' and data '{putLogEventsResponse.ResponseMetadata.FlattenedMetaData()}'.");
                }
                nextSequenceToken = putLogEventsResponse.NextSequenceToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending logs. No logs will be sent to AWS CloudWatch.");
                Console.WriteLine(ex);
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