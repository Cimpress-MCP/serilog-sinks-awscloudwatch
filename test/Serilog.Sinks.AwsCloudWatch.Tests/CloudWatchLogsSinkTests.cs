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
using Xunit.Abstractions;
using Serilog.Formatting;
using System.IO;

namespace Serilog.Sinks.AwsCloudWatch.Tests
{
    public class CloudWatchLogsSinkTests
    {
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private static Random random = new Random((int)DateTime.Now.Ticks);

        public CloudWatchLogsSinkTests(ITestOutputHelper output)
        {
            // so we can inspect what will be output to selflog
            Debugging.SelfLog.Enable(msg => output.WriteLine(msg));
        }

        [Fact(DisplayName = "EmitBatchAsync - Single batch")]
        public async Task SingleBatch()
        {
            // expect a single batch of events to be posted to CloudWatch Logs

            var client = new Mock<IAmazonCloudWatchLogs>(MockBehavior.Strict);

            var textFormatterMock = new Mock<ITextFormatter>(MockBehavior.Strict);
            textFormatterMock.Setup(s => s.Format(It.IsAny<LogEvent>(), It.IsAny<TextWriter>())).Callback((LogEvent l, TextWriter t) => l.RenderMessage(t));
            var options = new CloudWatchSinkOptions{ TextFormatter = textFormatterMock.Object };
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

            var putLogEventsCalls = new List<RequestCall<PutLogEventsRequest>>();
            client.Setup(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutLogEventsRequest, CancellationToken>((putLogEventsRequest, cancellationToken) => putLogEventsCalls.Add(new RequestCall<PutLogEventsRequest>(putLogEventsRequest))) // keep track of the requests made
                .ReturnsAsync(new PutLogEventsResponse
                {
                    HttpStatusCode = System.Net.HttpStatusCode.OK,
                    NextSequenceToken = Guid.NewGuid().ToString()
                });

            await sink.EmitBatchAsync(events);

            Assert.Single(putLogEventsCalls);

            var request = putLogEventsCalls.First().Request;
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
            var textFormatterMock = new Mock<ITextFormatter>(MockBehavior.Strict);
            textFormatterMock.Setup(s => s.Format(It.IsAny<LogEvent>(), It.IsAny<TextWriter>())).Callback((LogEvent l, TextWriter t) => l.RenderMessage(t));
            var options = new CloudWatchSinkOptions { LogGroupName = Guid.NewGuid().ToString(), TextFormatter = textFormatterMock.Object };
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

            var putLogEventsCalls = new List<RequestCall<PutLogEventsRequest>>();
            client.Setup(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutLogEventsRequest, CancellationToken>((putLogEventsRequest, cancellationToken) => putLogEventsCalls.Add(new RequestCall<PutLogEventsRequest>(putLogEventsRequest))) // keep track of the requests made
                .ReturnsAsync(new PutLogEventsResponse
                {
                    HttpStatusCode = System.Net.HttpStatusCode.OK,
                    NextSequenceToken = Guid.NewGuid().ToString()
                });

            await sink.EmitBatchAsync(events);

            Assert.Single(putLogEventsCalls);

            var request = putLogEventsCalls.First().Request;
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
            var textFormatterMock = new Mock<ITextFormatter>(MockBehavior.Strict);
            textFormatterMock.Setup(s => s.Format(It.IsAny<LogEvent>(), It.IsAny<TextWriter>())).Callback((LogEvent l, TextWriter t) => l.RenderMessage(t));
            var options = new CloudWatchSinkOptions { CreateLogGroup = false, TextFormatter = textFormatterMock.Object };
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

            var putLogEventsCalls = new List<RequestCall<PutLogEventsRequest>>();
            client.Setup(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutLogEventsRequest, CancellationToken>((putLogEventsRequest, cancellationToken) => putLogEventsCalls.Add(new RequestCall<PutLogEventsRequest>(putLogEventsRequest))) // keep track of the requests made
                .ReturnsAsync(new PutLogEventsResponse
                {
                    HttpStatusCode = System.Net.HttpStatusCode.OK,
                    NextSequenceToken = Guid.NewGuid().ToString()
                });

            await sink.EmitBatchAsync(events);

            Assert.Single(putLogEventsCalls);

            var request = putLogEventsCalls.First().Request;
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
            var textFormatterMock = new Mock<ITextFormatter>(MockBehavior.Strict);
            textFormatterMock.Setup(s => s.Format(It.IsAny<LogEvent>(), It.IsAny<TextWriter>())).Callback((LogEvent l, TextWriter t) => l.RenderMessage(t));
            var options = new CloudWatchSinkOptions { TextFormatter = textFormatterMock.Object };
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

            var putLogEventsCalls = new List<RequestCall<PutLogEventsRequest>>();
            client.Setup(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutLogEventsRequest, CancellationToken>((putLogEventsRequest, cancellationToken) => putLogEventsCalls.Add(new RequestCall<PutLogEventsRequest>(putLogEventsRequest))) // keep track of the requests made
                .ReturnsAsync(new PutLogEventsResponse
                {
                    HttpStatusCode = System.Net.HttpStatusCode.OK,
                    NextSequenceToken = Guid.NewGuid().ToString()
                });

            await sink.EmitBatchAsync(events);

            Assert.Single(putLogEventsCalls);

            var request = putLogEventsCalls.First().Request;
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
            var textFormatterMock = new Mock<ITextFormatter>(MockBehavior.Strict);
            textFormatterMock.Setup(s => s.Format(It.IsAny<LogEvent>(), It.IsAny<TextWriter>())).Callback((LogEvent l, TextWriter t) => l.RenderMessage(t));
            var options = new CloudWatchSinkOptions { TextFormatter = textFormatterMock.Object };
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

            var putLogEventsCalls = new List<RequestCall<PutLogEventsRequest>>();
            client.Setup(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutLogEventsRequest, CancellationToken>((putLogEventsRequest, cancellationToken) => putLogEventsCalls.Add(new RequestCall<PutLogEventsRequest>(putLogEventsRequest))) // keep track of the requests made
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
                var request = call.Request;

                Assert.Equal(options.LogGroupName, request.LogGroupName);
                Assert.Equal(events.Length / putLogEventsCalls.Count, request.LogEvents.Count);

                // make sure the events are ordered
                for (var index = 1; index < call.Request.LogEvents.Count; index++)
                {
                    Assert.True(call.Request.LogEvents.ElementAt(index).Timestamp >= call.Request.LogEvents.ElementAt(index - 1).Timestamp);
                }

                if (i == 0) // first call
                {
                    Assert.Null(request.SequenceToken);
                }
                else
                {
                    Assert.NotNull(request.SequenceToken);
                }
            }

            client.VerifyAll();
        }

        [Fact(DisplayName = "EmitBatchAsync - Beyond max batch count")]
        public async Task MoreThanMaxBatchCount()
        {
            // expect multiple batches, all having a batch count less than the maximum

            var client = new Mock<IAmazonCloudWatchLogs>(MockBehavior.Strict);
            var textFormatterMock = new Mock<ITextFormatter>(MockBehavior.Strict);
            textFormatterMock.Setup(s => s.Format(It.IsAny<LogEvent>(), It.IsAny<TextWriter>())).Callback((LogEvent l, TextWriter t) => l.RenderMessage(t));
            var options = new CloudWatchSinkOptions { TextFormatter = textFormatterMock.Object };
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

            var putLogEventsCalls = new List<RequestCall<PutLogEventsRequest>>();
            client.Setup(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutLogEventsRequest, CancellationToken>((putLogEventsRequest, cancellationToken) => putLogEventsCalls.Add(new RequestCall<PutLogEventsRequest>(putLogEventsRequest))) // keep track of the requests made
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
                var request = call.Request;

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
                }
            }

            client.VerifyAll();
        }

        [Fact(DisplayName = "EmitBatchAsync - Beyond batch size")]
        public async Task MoreThanMaxBatchSize()
        {
            // expect multiple batches, all having a batch size less than the maximum

            var client = new Mock<IAmazonCloudWatchLogs>(MockBehavior.Strict);
            var textFormatterMock = new Mock<ITextFormatter>(MockBehavior.Strict);
            textFormatterMock.Setup(s => s.Format(It.IsAny<LogEvent>(), It.IsAny<TextWriter>())).Callback((LogEvent l, TextWriter t) => l.RenderMessage(t));
            var options = new CloudWatchSinkOptions { TextFormatter = textFormatterMock.Object };
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

            var putLogEventsCalls = new List<RequestCall<PutLogEventsRequest>>();
            client.Setup(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutLogEventsRequest, CancellationToken>((putLogEventsRequest, cancellationToken) => putLogEventsCalls.Add(new RequestCall<PutLogEventsRequest>(putLogEventsRequest))) // keep track of the requests made
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
                var request = call.Request;

                Assert.Equal(options.LogGroupName, request.LogGroupName);

                if (i == 0) // first call
                {
                    Assert.Null(request.SequenceToken);
                    Assert.Equal(203, request.LogEvents.Count); // expect 203 of the 256 messages in the first batch
                }
                else
                {
                    Assert.NotNull(request.SequenceToken);
                    Assert.Equal(53, request.LogEvents.Count); // expect 53 of the 256 messages in the second batch
                }
            }

            client.VerifyAll();
        }

        [Fact(DisplayName = "EmitBatchAsync - Service unavailable")]
        public async Task ServiceUnavailable()
        {
            // expect retries until exhausted

            var client = new Mock<IAmazonCloudWatchLogs>(MockBehavior.Strict);
            var textFormatterMock = new Mock<ITextFormatter>(MockBehavior.Strict);
            textFormatterMock.Setup(s => s.Format(It.IsAny<LogEvent>(), It.IsAny<TextWriter>())).Callback((LogEvent l, TextWriter t) => l.RenderMessage(t));
            var options = new CloudWatchSinkOptions { TextFormatter = textFormatterMock.Object };
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

            var putLogEventsCalls = new List<RequestCall<PutLogEventsRequest>>();
            client.Setup(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutLogEventsRequest, CancellationToken>((putLogEventsRequest, cancellationToken) => putLogEventsCalls.Add(new RequestCall<PutLogEventsRequest>(putLogEventsRequest))) // keep track of the requests made
                .ThrowsAsync(new ServiceUnavailableException("unavailable"));

            await sink.EmitBatchAsync(events);

            Assert.Equal(options.RetryAttempts + 1, putLogEventsCalls.Count);

            var lastInterval = TimeSpan.Zero;
            for (var i = 1; i < putLogEventsCalls.Count; i++)
            {
                // ensure retry attempts are throttled properly
                var interval = putLogEventsCalls[i].DateTime.Subtract(putLogEventsCalls[i - 1].DateTime);
                Assert.True(interval.TotalMilliseconds + 5 >= (CloudWatchLogSink.ErrorBackoffStartingInterval.Milliseconds * Math.Pow(2, i - 1)), $"{interval.TotalMilliseconds} >= {CloudWatchLogSink.ErrorBackoffStartingInterval.Milliseconds * Math.Pow(2, i - 1)}");
                lastInterval = interval;
            }
            
            client.VerifyAll();
        }

        [Fact(DisplayName = "EmitBatchAsync - Service unavailable with eventual success")]
        public async Task ServiceUnavailable_WithEventualSuccess()
        {
            // expect successful posting of batch after retry

            var client = new Mock<IAmazonCloudWatchLogs>(MockBehavior.Strict);
            var textFormatterMock = new Mock<ITextFormatter>(MockBehavior.Strict);
            textFormatterMock.Setup(s => s.Format(It.IsAny<LogEvent>(), It.IsAny<TextWriter>())).Callback((LogEvent l, TextWriter t) => l.RenderMessage(t));
            var options = new CloudWatchSinkOptions { TextFormatter = textFormatterMock.Object };
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

            client.SetupSequence(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceUnavailableException("unavailable"))
                .ReturnsAsync(new PutLogEventsResponse { HttpStatusCode = System.Net.HttpStatusCode.OK, NextSequenceToken = Guid.NewGuid().ToString() });

            await sink.EmitBatchAsync(events);

            client.Verify(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            client.VerifyAll();
        }

        [Fact(DisplayName = "EmitBatchAsync - Resource not found")]
        public async Task ResourceNotFound()
        {
            // expect failure, creation of log group/stream, and evenutal success

            var client = new Mock<IAmazonCloudWatchLogs>(MockBehavior.Strict);
            var textFormatterMock = new Mock<ITextFormatter>(MockBehavior.Strict);
            textFormatterMock.Setup(s => s.Format(It.IsAny<LogEvent>(), It.IsAny<TextWriter>())).Callback((LogEvent l, TextWriter t) => l.RenderMessage(t));
            var options = new CloudWatchSinkOptions { TextFormatter = textFormatterMock.Object };
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

            List<CreateLogStreamRequest> createLogStreamRequests = new List<CreateLogStreamRequest>();
            client.Setup(mock => mock.CreateLogStreamAsync(It.IsAny<CreateLogStreamRequest>(), It.IsAny<CancellationToken>()))
                .Callback<CreateLogStreamRequest, CancellationToken>((createLogStreamRequest, cancellationToken) => createLogStreamRequests.Add(createLogStreamRequest))
                .ReturnsAsync(new CreateLogStreamResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            client.SetupSequence(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceNotFoundException("no resource"))
                .ReturnsAsync(new PutLogEventsResponse { HttpStatusCode = System.Net.HttpStatusCode.OK, NextSequenceToken = Guid.NewGuid().ToString() });

            await sink.EmitBatchAsync(events);

            client.Verify(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            client.Verify(mock => mock.DescribeLogGroupsAsync(It.IsAny<DescribeLogGroupsRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            client.Verify(mock => mock.CreateLogGroupAsync(It.Is<CreateLogGroupRequest>(req => req.LogGroupName == options.LogGroupName), It.IsAny<CancellationToken>()), Times.Exactly(2));
            client.Verify(mock => mock.CreateLogStreamAsync(It.IsAny<CreateLogStreamRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

            Assert.Equal(createLogStreamRequests.ElementAt(0).LogStreamName, createLogStreamRequests.ElementAt(1).LogStreamName);

            client.VerifyAll();
        }

        [Fact(DisplayName = "EmitBatchAsync - Unable to create resource")]
        public async Task ResourceNotFound_CannotCreateResource()
        {
            // expect failure with failure to successfully create resources upon retries

            var client = new Mock<IAmazonCloudWatchLogs>(MockBehavior.Strict);
            var textFormatterMock = new Mock<ITextFormatter>(MockBehavior.Strict);
            textFormatterMock.Setup(s => s.Format(It.IsAny<LogEvent>(), It.IsAny<TextWriter>())).Callback((LogEvent l, TextWriter t) => l.RenderMessage(t));
            var options = new CloudWatchSinkOptions { TextFormatter = textFormatterMock.Object };
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

            client.SetupSequence(mock => mock.CreateLogStreamAsync(It.IsAny<CreateLogStreamRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateLogStreamResponse { HttpStatusCode = System.Net.HttpStatusCode.OK })
                .ThrowsAsync(new Exception("can't create a new log stream"));

            client.Setup(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceNotFoundException("no resource"));

            await sink.EmitBatchAsync(events);

            client.Verify(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            client.Verify(mock => mock.DescribeLogGroupsAsync(It.IsAny<DescribeLogGroupsRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            client.Verify(mock => mock.CreateLogGroupAsync(It.Is<CreateLogGroupRequest>(req => req.LogGroupName == options.LogGroupName), It.IsAny<CancellationToken>()), Times.Exactly(2));
            client.Verify(mock => mock.CreateLogStreamAsync(It.IsAny<CreateLogStreamRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

            client.VerifyAll();
        }

        [Fact(DisplayName = "EmitBatchAsync - Invalid parameter")]
        public async Task InvalidParameter()
        {
            // expect batch dropped

            var client = new Mock<IAmazonCloudWatchLogs>(MockBehavior.Strict);
            var textFormatterMock = new Mock<ITextFormatter>(MockBehavior.Strict);
            textFormatterMock.Setup(s => s.Format(It.IsAny<LogEvent>(), It.IsAny<TextWriter>())).Callback((LogEvent l, TextWriter t) => l.RenderMessage(t));
            var options = new CloudWatchSinkOptions { TextFormatter = textFormatterMock.Object };
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

            var putLogEventsCalls = new List<RequestCall<PutLogEventsRequest>>();
            client.Setup(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutLogEventsRequest, CancellationToken>((putLogEventsRequest, cancellationToken) => putLogEventsCalls.Add(new RequestCall<PutLogEventsRequest>(putLogEventsRequest))) // keep track of the requests made
                .ThrowsAsync(new InvalidParameterException("invalid param"));

            await sink.EmitBatchAsync(events);

            Assert.Single(putLogEventsCalls);

            client.VerifyAll();
        }

        [Fact(DisplayName = "EmitBatchAsync - Invalid sequence token")]
        public async Task InvalidSequenceToken()
        {
            // expect update of sequence token and successful retry

            var client = new Mock<IAmazonCloudWatchLogs>(MockBehavior.Strict);
            var textFormatterMock = new Mock<ITextFormatter>(MockBehavior.Strict);
            textFormatterMock.Setup(s => s.Format(It.IsAny<LogEvent>(), It.IsAny<TextWriter>())).Callback((LogEvent l, TextWriter t) => l.RenderMessage(t));
            var options = new CloudWatchSinkOptions { TextFormatter = textFormatterMock.Object };
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

            client.Setup(mock => mock.DescribeLogStreamsAsync(It.IsAny<DescribeLogStreamsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DescribeLogStreamsResponse { HttpStatusCode = System.Net.HttpStatusCode.OK, NextToken = Guid.NewGuid().ToString() });

            List<CreateLogStreamRequest> createLogStreamRequests = new List<CreateLogStreamRequest>();
            client.Setup(mock => mock.CreateLogStreamAsync(It.IsAny<CreateLogStreamRequest>(), It.IsAny<CancellationToken>()))
                .Callback<CreateLogStreamRequest, CancellationToken>((createLogStreamRequest, cancellationToken) => createLogStreamRequests.Add(createLogStreamRequest))
                .ReturnsAsync(new CreateLogStreamResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            client.SetupSequence(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidSequenceTokenException("invalid sequence"))
                .ReturnsAsync(new PutLogEventsResponse { HttpStatusCode = System.Net.HttpStatusCode.OK, NextSequenceToken = Guid.NewGuid().ToString() });

            await sink.EmitBatchAsync(events);

            client.Verify(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            client.Verify(mock => mock.DescribeLogStreamsAsync(It.Is<DescribeLogStreamsRequest>(req => req.LogGroupName == options.LogGroupName && req.LogStreamNamePrefix == createLogStreamRequests.First().LogStreamName), It.IsAny<CancellationToken>()), Times.Once);
            client.Verify(mock => mock.CreateLogStreamAsync(It.IsAny<CreateLogStreamRequest>(), It.IsAny<CancellationToken>()), Times.Once);

            client.VerifyAll();
        }

        [Fact(DisplayName = "EmitBatchAsync - Invalid sequence token with new log stream")]
        public async Task InvalidSequenceToken_CannotUpdateSequenceToken()
        {
            // expect update of sequence token and success on a new log stream

            var logStreamNameProvider = new Mock<ILogStreamNameProvider>();
            logStreamNameProvider.SetupSequence(mock => mock.GetLogStreamName())
                .Returns("a")
                .Returns("b");

            var client = new Mock<IAmazonCloudWatchLogs>(MockBehavior.Strict);
            var textFormatterMock = new Mock<ITextFormatter>(MockBehavior.Strict);
            textFormatterMock.Setup(s => s.Format(It.IsAny<LogEvent>(), It.IsAny<TextWriter>())).Callback((LogEvent l, TextWriter t) => l.RenderMessage(t));
            var options = new CloudWatchSinkOptions { LogStreamNameProvider = logStreamNameProvider.Object, TextFormatter = textFormatterMock.Object };
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

            client.Setup(mock => mock.DescribeLogStreamsAsync(It.IsAny<DescribeLogStreamsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("no describe log stream"));

            List<CreateLogStreamRequest> createLogStreamRequests = new List<CreateLogStreamRequest>();
            client.Setup(mock => mock.CreateLogStreamAsync(It.IsAny<CreateLogStreamRequest>(), It.IsAny<CancellationToken>()))
                .Callback<CreateLogStreamRequest, CancellationToken>((createLogStreamRequest, cancellationToken) => createLogStreamRequests.Add(createLogStreamRequest))
                .ReturnsAsync(new CreateLogStreamResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            client.Setup(mock => mock.PutLogEventsAsync(It.Is<PutLogEventsRequest>(req => req.LogStreamName == "a"), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidSequenceTokenException("invalid sequence"));

            client.Setup(mock => mock.PutLogEventsAsync(It.Is<PutLogEventsRequest>(req => req.LogStreamName == "b"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutLogEventsResponse { HttpStatusCode = System.Net.HttpStatusCode.OK, NextSequenceToken = Guid.NewGuid().ToString() });

            await sink.EmitBatchAsync(events);

            client.Verify(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            client.Verify(mock => mock.DescribeLogStreamsAsync(It.Is<DescribeLogStreamsRequest>(req => req.LogGroupName == options.LogGroupName && req.LogStreamNamePrefix == createLogStreamRequests.First().LogStreamName), It.IsAny<CancellationToken>()), Times.Once);
            client.Verify(mock => mock.CreateLogStreamAsync(It.IsAny<CreateLogStreamRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

            Assert.Equal(2, createLogStreamRequests.Count);
            Assert.NotEqual(createLogStreamRequests.ElementAt(0).LogStreamName, createLogStreamRequests.ElementAt(1).LogStreamName);

            client.VerifyAll();
        }

        [Fact(DisplayName = "EmitBatchAsync - Data already accepted")]
        public async Task DataAlreadyAccepted()
        {
            // expect update of sequence token and successful retry

            var client = new Mock<IAmazonCloudWatchLogs>(MockBehavior.Strict);
            var textFormatterMock = new Mock<ITextFormatter>(MockBehavior.Strict);
            textFormatterMock.Setup(s => s.Format(It.IsAny<LogEvent>(), It.IsAny<TextWriter>())).Callback((LogEvent l, TextWriter t) => l.RenderMessage(t));
            var options = new CloudWatchSinkOptions { TextFormatter = textFormatterMock.Object };
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

            client.Setup(mock => mock.DescribeLogStreamsAsync(It.IsAny<DescribeLogStreamsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DescribeLogStreamsResponse { HttpStatusCode = System.Net.HttpStatusCode.OK, NextToken = Guid.NewGuid().ToString() });

            List<CreateLogStreamRequest> createLogStreamRequests = new List<CreateLogStreamRequest>();
            client.Setup(mock => mock.CreateLogStreamAsync(It.IsAny<CreateLogStreamRequest>(), It.IsAny<CancellationToken>()))
                .Callback<CreateLogStreamRequest, CancellationToken>((createLogStreamRequest, cancellationToken) => createLogStreamRequests.Add(createLogStreamRequest))
                .ReturnsAsync(new CreateLogStreamResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

            client.SetupSequence(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DataAlreadyAcceptedException("data already accepted"))
                .ReturnsAsync(new PutLogEventsResponse { HttpStatusCode = System.Net.HttpStatusCode.OK, NextSequenceToken = Guid.NewGuid().ToString() });

            await sink.EmitBatchAsync(events);

            client.Verify(mock => mock.PutLogEventsAsync(It.IsAny<PutLogEventsRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            client.Verify(mock => mock.DescribeLogStreamsAsync(It.Is<DescribeLogStreamsRequest>(req => req.LogGroupName == options.LogGroupName && req.LogStreamNamePrefix == createLogStreamRequests.First().LogStreamName), It.IsAny<CancellationToken>()), Times.Once);
            client.Verify(mock => mock.CreateLogStreamAsync(It.IsAny<CreateLogStreamRequest>(), It.IsAny<CancellationToken>()), Times.Once);

            client.VerifyAll();
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

        /// <summary>
        /// Private class to keep track of calls made to CloudWatch Logs.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <remarks>
        /// Necessary because ValueTuple isn't supported until .NET 4.7, and we want to test at .NET 4.5.2.
        /// </remarks>
        private class RequestCall<T>
            where T : AmazonCloudWatchLogsRequest
        {
            public RequestCall(T request)
            {
                Request = request;
                DateTime = DateTime.UtcNow;
            }

            public T Request { get; private set; }
            public DateTime DateTime { get; private set; }
        }
    }
}
