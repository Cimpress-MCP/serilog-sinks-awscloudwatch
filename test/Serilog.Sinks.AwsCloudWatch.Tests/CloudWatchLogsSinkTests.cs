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
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private static Random random = new Random((int)DateTime.Now.Ticks);

        [Fact(DisplayName = "EmitBatchAsync - Single batch")]
        public async Task SingleBatch()
        {
            // expect a single batch of events to be posted to CloudWatch Logs

            var client = new Mock<IAmazonCloudWatchLogs>(MockBehavior.Strict);
            var options = new CloudWatchSinkOptions();
            var sink = new CloudWatchLogSink(client.Object, options);
            var events = Enumerable.Range(0, 10)
                .Select(_ => // create 10 events with message length of 12
                    new LogEvent(
                        DateTimeOffset.UtcNow,
                        LogEventLevel.Information,
                        null,
                        new MessageTemplateParser().Parse(CreateMessage(12)),
                        Enumerable.Empty<LogEventProperty>()))
                .ToArray();

            client.Setup(mock => mock.DescribeLogGroupsAsync(It.IsAny<DescribeLogGroupsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DescribeLogGroupsResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            client.Setup(mock => mock.CreateLogGroupAsync(It.IsAny<CreateLogGroupRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateLogGroupResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            client.Setup(mock => mock.CreateLogStreamAsync(It.IsAny<CreateLogStreamRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateLogStreamResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            var putLogEventsCalls = new List<(PutLogEventsRequest request, CancellationToken cancellationToken)>();
            client.Setup(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutLogEventsRequest, CancellationToken>((putLogEventsRequest, cancellationToken) => putLogEventsCalls.Add((putLogEventsRequest, cancellationToken))) // keep track of the requests made
                .ReturnsAsync(new PutLogEventsResponse
                {
                    HttpStatusCode = System.Net.HttpStatusCode.OK,
                    NextSequenceToken = Guid.NewGuid().ToString()
                });

            await sink.EmitBatchAsync(events);

            Assert.Single(putLogEventsCalls);

            var request = putLogEventsCalls.First().request;
            Assert.Equal(options.LogGroupName, request.LogGroupName);
            Assert.Null(request.SequenceToken);
            Assert.Equal(10, request.LogEvents.Count);
            for (var i = 0; i < events.Length; i++)
            {
                Assert.Equal(events[i].MessageTemplate.Text, request.LogEvents.ElementAt(i).Message);
            }

            client.VerifyAll();
        }

        [Fact(DisplayName = "EmitBatchAsync - Single batch (log group exists)")]
        public async Task SingleBatch_LogGroupExists()
        {
            // expect a single batch of events to be posted to CloudWatch Logs

            var client = new Mock<IAmazonCloudWatchLogs>(MockBehavior.Strict);
            var options = new CloudWatchSinkOptions { LogGroupName = Guid.NewGuid().ToString() };
            var sink = new CloudWatchLogSink(client.Object, options);
            var events = Enumerable.Range(0, 10)
                .Select(_ => // create 10 events with message length of 12
                    new LogEvent(
                        DateTimeOffset.UtcNow,
                        LogEventLevel.Information,
                        null,
                        new MessageTemplateParser().Parse(CreateMessage(12)),
                        Enumerable.Empty<LogEventProperty>()))
                .ToArray();

            client.Setup(mock => mock.DescribeLogGroupsAsync(It.IsAny<DescribeLogGroupsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DescribeLogGroupsResponse
                {
                    HttpStatusCode = System.Net.HttpStatusCode.OK,
                    LogGroups = new List<LogGroup>
                    {
                        new LogGroup
                        {
                            LogGroupName = options.LogGroupName
                        }
                    }
                });

            client.Setup(mock => mock.CreateLogStreamAsync(It.IsAny<CreateLogStreamRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateLogStreamResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            var putLogEventsCalls = new List<(PutLogEventsRequest request, CancellationToken cancellationToken)>();
            client.Setup(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutLogEventsRequest, CancellationToken>((putLogEventsRequest, cancellationToken) => putLogEventsCalls.Add((putLogEventsRequest, cancellationToken))) // keep track of the requests made
                .ReturnsAsync(new PutLogEventsResponse
                {
                    HttpStatusCode = System.Net.HttpStatusCode.OK,
                    NextSequenceToken = Guid.NewGuid().ToString()
                });

            await sink.EmitBatchAsync(events);

            Assert.Single(putLogEventsCalls);

            var request = putLogEventsCalls.First().request;
            Assert.Equal(options.LogGroupName, request.LogGroupName);
            Assert.Null(request.SequenceToken);
            Assert.Equal(10, request.LogEvents.Count);
            for (var i = 0; i < events.Length; i++)
            {
                Assert.Equal(events[i].MessageTemplate.Text, request.LogEvents.ElementAt(i).Message);
            }

            client.VerifyAll();
        }

        [Fact(DisplayName = "EmitBatchAsync - Single batch (do not create log group)")]
        public async Task SingleBatch_WithoutCreatingLogGroup()
        {
            // expect a single batch of events to be posted to CloudWatch Logs

            var client = new Mock<IAmazonCloudWatchLogs>(MockBehavior.Strict);
            var options = new CloudWatchSinkOptions { CreateLogGroup = false };
            var sink = new CloudWatchLogSink(client.Object, options);
            var events = Enumerable.Range(0, 10)
                .Select(_ => // create 10 events with message length of 12
                    new LogEvent(
                        DateTimeOffset.UtcNow,
                        LogEventLevel.Information,
                        null,
                        new MessageTemplateParser().Parse(CreateMessage(12)),
                        Enumerable.Empty<LogEventProperty>()))
                .ToArray();

            client.Setup(mock => mock.CreateLogStreamAsync(It.IsAny<CreateLogStreamRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateLogStreamResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            var putLogEventsCalls = new List<(PutLogEventsRequest request, CancellationToken cancellationToken)>();
            client.Setup(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutLogEventsRequest, CancellationToken>((putLogEventsRequest, cancellationToken) => putLogEventsCalls.Add((putLogEventsRequest, cancellationToken))) // keep track of the requests made
                .ReturnsAsync(new PutLogEventsResponse
                {
                    HttpStatusCode = System.Net.HttpStatusCode.OK,
                    NextSequenceToken = Guid.NewGuid().ToString()
                });

            await sink.EmitBatchAsync(events);

            Assert.Single(putLogEventsCalls);

            var request = putLogEventsCalls.First().request;
            Assert.Equal(options.LogGroupName, request.LogGroupName);
            Assert.Null(request.SequenceToken);
            Assert.Equal(10, request.LogEvents.Count);
            for (var i = 0; i < events.Length; i++)
            {
                Assert.Equal(events[i].MessageTemplate.Text, request.LogEvents.ElementAt(i).Message);
            }

            client.VerifyAll();
        }

        [Fact(DisplayName = "EmitBatchAsync - Large message")]
        public async Task LargeMessage()
        {
            // expect an event with a length beyond the MaxLogEventSize will be truncated to the MaxLogEventSize

            var client = new Mock<IAmazonCloudWatchLogs>(MockBehavior.Strict);
            var options = new CloudWatchSinkOptions();
            var sink = new CloudWatchLogSink(client.Object, options);
            var largeEventMessage = CreateMessage(CloudWatchLogSink.MaxLogEventSize + 1);
            var events = new LogEvent[]
            {
                new LogEvent(
                    DateTimeOffset.UtcNow,
                    LogEventLevel.Information,
                    null,
                    new MessageTemplateParser().Parse(largeEventMessage),
                    Enumerable.Empty<LogEventProperty>())
            };

            client.Setup(mock => mock.DescribeLogGroupsAsync(It.IsAny<DescribeLogGroupsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DescribeLogGroupsResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            client.Setup(mock => mock.CreateLogGroupAsync(It.IsAny<CreateLogGroupRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateLogGroupResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            client.Setup(mock => mock.CreateLogStreamAsync(It.IsAny<CreateLogStreamRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateLogStreamResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            var putLogEventsCalls = new List<(PutLogEventsRequest request, CancellationToken cancellationToken)>();
            client.Setup(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutLogEventsRequest, CancellationToken>((putLogEventsRequest, cancellationToken) => putLogEventsCalls.Add((putLogEventsRequest, cancellationToken))) // keep track of the requests made
                .ReturnsAsync(new PutLogEventsResponse
                {
                    HttpStatusCode = System.Net.HttpStatusCode.OK,
                    NextSequenceToken = Guid.NewGuid().ToString()
                });

            await sink.EmitBatchAsync(events);

            Assert.Single(putLogEventsCalls);

            var request = putLogEventsCalls.First().request;
            Assert.Equal(options.LogGroupName, request.LogGroupName);
            Assert.Null(request.SequenceToken);
            Assert.Single(request.LogEvents);
            Assert.Equal(largeEventMessage.Substring(0, CloudWatchLogSink.MaxLogEventSize), request.LogEvents.First().Message);

            client.VerifyAll();
        }

        [Fact(DisplayName = "EmitBatchAsync - Beyond batch span")]
        public async Task MultipleDays()
        {
            // expect a batch to be posted for each 24-hour period

            var client = new Mock<IAmazonCloudWatchLogs>(MockBehavior.Strict);
            var options = new CloudWatchSinkOptions();
            var sink = new CloudWatchLogSink(client.Object, options);
            var events = Enumerable.Range(0, 20)
                .Select(i => // create multipe events with message length of 12
                    new LogEvent(
                        DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays((i % 2) * 2)), // split the events into two days
                        LogEventLevel.Information,
                        null,
                        new MessageTemplateParser().Parse(CreateMessage(12)),
                        Enumerable.Empty<LogEventProperty>()))
                .ToArray();

            client.Setup(mock => mock.DescribeLogGroupsAsync(It.IsAny<DescribeLogGroupsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DescribeLogGroupsResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            client.Setup(mock => mock.CreateLogGroupAsync(It.IsAny<CreateLogGroupRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateLogGroupResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            client.Setup(mock => mock.CreateLogStreamAsync(It.IsAny<CreateLogStreamRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateLogStreamResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            var putLogEventsCalls = new List<(PutLogEventsRequest request, CancellationToken cancellationToken, DateTime datetime)>();
            client.Setup(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutLogEventsRequest, CancellationToken>((putLogEventsRequest, cancellationToken) => putLogEventsCalls.Add((putLogEventsRequest, cancellationToken, DateTime.UtcNow))) // keep track of the requests made
                .ReturnsAsync(new PutLogEventsResponse
                {
                    HttpStatusCode = System.Net.HttpStatusCode.OK,
                    NextSequenceToken = Guid.NewGuid().ToString()
                });

            await sink.EmitBatchAsync(events);

            Assert.Equal(2, putLogEventsCalls.Count);

            for (var i = 0; i < putLogEventsCalls.Count; i++)
            {
                var call = putLogEventsCalls[i];
                var request = call.request;

                Assert.Equal(options.LogGroupName, request.LogGroupName);
                Assert.Equal(events.Length / putLogEventsCalls.Count, request.LogEvents.Count);

                // make sure the events are ordered
                for (var index = 1; index < call.request.LogEvents.Count; index++)
                {
                    Assert.True(call.request.LogEvents.ElementAt(index).Timestamp > call.request.LogEvents.ElementAt(index - 1).Timestamp);
                }

                if (i == 0) // first call
                {
                    Assert.Null(request.SequenceToken);
                }
                else
                {
                    Assert.NotNull(request.SequenceToken);

                    // ensure calls are throttled
                    var interval = call.datetime.Subtract(putLogEventsCalls[i - 1].datetime);
                    Assert.True(interval >= CloudWatchLogSink.ThrottlingInterval);
                }
            }

            client.VerifyAll();
        }

        [Fact(DisplayName = "EmitBatchAsync - Beyond max batch count")]
        public async Task MoreThanMaxBatchCount()
        {
            // expect multiple batches, all having a batch count less than the maximum

            var client = new Mock<IAmazonCloudWatchLogs>(MockBehavior.Strict);
            var options = new CloudWatchSinkOptions();
            var sink = new CloudWatchLogSink(client.Object, options);
            var events = Enumerable.Range(0, CloudWatchLogSink.MaxLogEventBatchCount + 1)
                .Select(i => 
                    new LogEvent(
                        DateTimeOffset.UtcNow,
                        LogEventLevel.Information,
                        null,
                        new MessageTemplateParser().Parse(CreateMessage(2)), // make sure size is not an issue
                        Enumerable.Empty<LogEventProperty>()))
                .ToArray();

            client.Setup(mock => mock.DescribeLogGroupsAsync(It.IsAny<DescribeLogGroupsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DescribeLogGroupsResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            client.Setup(mock => mock.CreateLogGroupAsync(It.IsAny<CreateLogGroupRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateLogGroupResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            client.Setup(mock => mock.CreateLogStreamAsync(It.IsAny<CreateLogStreamRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateLogStreamResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            var putLogEventsCalls = new List<(PutLogEventsRequest request, CancellationToken cancellationToken, DateTime datetime)>();
            client.Setup(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutLogEventsRequest, CancellationToken>((putLogEventsRequest, cancellationToken) => putLogEventsCalls.Add((putLogEventsRequest, cancellationToken, DateTime.UtcNow))) // keep track of the requests made
                .ReturnsAsync(new PutLogEventsResponse
                {
                    HttpStatusCode = System.Net.HttpStatusCode.OK,
                    NextSequenceToken = Guid.NewGuid().ToString()
                });

            await sink.EmitBatchAsync(events);

            Assert.Equal(2, putLogEventsCalls.Count);

            for (var i = 0; i < putLogEventsCalls.Count; i++)
            {
                var call = putLogEventsCalls[i];
                var request = call.request;

                Assert.Equal(options.LogGroupName, request.LogGroupName);

                if (i == 0) // first call
                {
                    Assert.Null(request.SequenceToken);
                    Assert.Equal(CloudWatchLogSink.MaxLogEventBatchCount, request.LogEvents.Count);
                }
                else
                {
                    Assert.NotNull(request.SequenceToken);
                    Assert.Single(request.LogEvents);

                    // ensure calls are throttled
                    var interval = call.datetime.Subtract(putLogEventsCalls[i - 1].datetime);
                    Assert.True(interval >= CloudWatchLogSink.ThrottlingInterval);
                }
            }

            client.VerifyAll();
        }

        [Fact(DisplayName = "EmitBatchAsync - Beyond batch size")]
        public async Task MoreThanMaxBatchSize()
        {
            // expect multiple batches, all having a batch size less than the maximum

            var client = new Mock<IAmazonCloudWatchLogs>(MockBehavior.Strict);
            var options = new CloudWatchSinkOptions();
            var sink = new CloudWatchLogSink(client.Object, options);
            var events = Enumerable.Range(0, 256) // 256 4 KB messages matches our max batch size, but we want to test a "less nice" scenario, so we'll create 256 5 KB messages
                .Select(i =>
                    new LogEvent(
                        DateTimeOffset.UtcNow,
                        LogEventLevel.Information,
                        null,
                        new MessageTemplateParser().Parse(CreateMessage(1024 * 5)), // 5 KB messages
                        Enumerable.Empty<LogEventProperty>()))
                .ToArray();

            client.Setup(mock => mock.DescribeLogGroupsAsync(It.IsAny<DescribeLogGroupsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DescribeLogGroupsResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            client.Setup(mock => mock.CreateLogGroupAsync(It.IsAny<CreateLogGroupRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateLogGroupResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            client.Setup(mock => mock.CreateLogStreamAsync(It.IsAny<CreateLogStreamRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateLogStreamResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            var putLogEventsCalls = new List<(PutLogEventsRequest request, CancellationToken cancellationToken, DateTime datetime)>();
            client.Setup(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutLogEventsRequest, CancellationToken>((putLogEventsRequest, cancellationToken) => putLogEventsCalls.Add((putLogEventsRequest, cancellationToken, DateTime.UtcNow))) // keep track of the requests made
                .ReturnsAsync(new PutLogEventsResponse
                {
                    HttpStatusCode = System.Net.HttpStatusCode.OK,
                    NextSequenceToken = Guid.NewGuid().ToString()
                });

            await sink.EmitBatchAsync(events);

            Assert.Equal(2, putLogEventsCalls.Count);

            for (var i = 0; i < putLogEventsCalls.Count; i++)
            {
                var call = putLogEventsCalls[i];
                var request = call.request;

                Assert.Equal(options.LogGroupName, request.LogGroupName);

                if (i == 0) // first call
                {
                    Assert.Null(request.SequenceToken);
                    Assert.Equal(204, request.LogEvents.Count); // expect 204 of the 256 messages in the first batch
                }
                else
                {
                    Assert.NotNull(request.SequenceToken);
                    Assert.Equal(52, request.LogEvents.Count); // expect 52 of the 256 messages in the second batch

                    // ensure calls are throttled
                    var interval = call.datetime.Subtract(putLogEventsCalls[i - 1].datetime);
                    Assert.True(interval >= CloudWatchLogSink.ThrottlingInterval);
                }
            }

            client.VerifyAll();
        }

        [Fact(DisplayName = "EmitBatchAsync - Service unavailable", Skip = "Implement")]
        public async Task ServiceUnavailable()
        {
            // expect retries until exhausted
        }

        [Fact(DisplayName = "EmitBatchAsync - Service unavailable with eventual success", Skip = "Implement")]
        public async Task ServiceUnavailable_WithEventualSuccess()
        {
            // expect successful posting of batch after retry
        }

        [Fact(DisplayName = "EmitBatchAsync - Resource not found", Skip = "Implement")]
        public async Task ResourceNotFound()
        {
            // expect failure, creation of log group/stream, and evenutal success
        }

        [Fact(DisplayName = "EmitBatchAsync - Unable to create resource", Skip = "Implement")]
        public async Task ResourceNotFound_CannotCreateResource()
        {
            // expect failure with failure to successfully create resources upon retries
        }

        [Fact(DisplayName = "EmitBatchAsync - Invalid parameter", Skip = "Implement")]
        public async Task InvalidParameter()
        {
            // expect batch dropped
        }

        [Fact(DisplayName = "EmitBatchAsync - Invalid sequence token", Skip = "Implement")]
        public async Task InvalidSequenceToken()
        {
            // expect update of sequence token and successful retry
        }

        [Fact(DisplayName = "EmitBatchAsync - Invalid sequence token with new log stream", Skip = "Implement")]
        public async Task InvalidSequenceToken_CannotUpdateSequenceToken()
        {
            // expect update of sequence token and successful retry
        }

        [Fact(DisplayName = "EmitBatchAsync - Data already accepted", Skip = "Implement")]
        public async Task DataAlreadyAccepted()
        {
            // expect update of sequence token and successful retry
        }



        /// <summary>
        /// Creates a message of random characters of the given size.
        /// </summary>
        /// <param name="size">The size of the message.</param>
        /// <returns>A string consisting of random characters from the alphabet.</returns>
        private string CreateMessage(int size)
        {
            var message = new string(Enumerable.Range(0, size).Select(_ => Alphabet[random.Next(0, Alphabet.Length)]).ToArray());
            Assert.Equal(size, message.Length);
            return message;
        }
    }
}
