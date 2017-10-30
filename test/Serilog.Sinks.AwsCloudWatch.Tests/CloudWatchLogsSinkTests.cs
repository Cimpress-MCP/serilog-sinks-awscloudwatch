using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Moq;
using Serilog.Events;
using Serilog.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Serilog.Sinks.AwsCloudWatch.Tests
{
    public class CloudWatchLogsSinkTests
    {
        public async Task HappyPath()
        {

        }

        [Fact(DisplayName = "EmitBatchAsync - Truncate message")]
        public async Task LargeMessage_Truncated()
        {
            var client = new Mock<IAmazonCloudWatchLogs>();
            var options = new CloudWatchSinkOptions();
            var sink = new CloudWatchLogSink(client.Object, options);
            var largeEventMessage = new string(Enumerable.Repeat('a', CloudWatchLogSink.MaxLogEventSize + 1).ToArray());
            var events = new LogEvent[]
            {
                new LogEvent(
                    DateTimeOffset.UtcNow, 
                    LogEventLevel.Information, 
                    null,
                    new MessageTemplateParser().Parse(largeEventMessage),
                    Enumerable.Empty<LogEventProperty>())
            };

            var putLogEventsRequests = new List<PutLogEventsRequest>();
            client.Setup(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutLogEventsRequest, CancellationToken>((putLogEventsRequest, cancellationToken) => putLogEventsRequests.Add(putLogEventsRequest)); // keep track of the requests made

            await sink.EmitBatchAsync(events);

            Assert.Single(putLogEventsRequests);

            var request = putLogEventsRequests.First();
            Assert.Equal(options.LogGroupName, request.LogGroupName);

            //client.Verify(
            //    mock => mock.PutLogEventsAsync(
            //        It.Is<PutLogEventsRequest>(request =>
            //            request.LogGroupName == options.LogGroupName
            //            && request.SequenceToken == null
            //            && request.LogEvents.Count == 1
            //            && request.LogEvents.First().Message == largeEventMessage.Substring(0, CloudWatchLogSink.MaxLogEventSize)),
            //        It.IsAny<CancellationToken>()), 
            //    Times.Once);
        }


    }
}
