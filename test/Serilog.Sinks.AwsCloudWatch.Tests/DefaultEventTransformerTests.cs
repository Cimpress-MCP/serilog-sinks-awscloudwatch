using Moq;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Parsing;
using Serilog.Sinks.AwsCloudWatch.EventTransformer;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Serilog.Sinks.AwsCloudWatch.Tests
{
    public class DefaultEventTransformerTests
    {
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        [Fact(DisplayName = "TransformEvent")]
        public void TransformEvent()
        {
            var eventMessage = CloudWatchLogsSinkTests.CreateMessage(16, Alphabet);

            var eventDateTime = DateTimeOffset.Now;
            var transformer = new DefaultEventTransformer();
            var @event = new LogEvent(eventDateTime, LogEventLevel.Information, null, new MessageTemplateParser().Parse(eventMessage), Enumerable.Empty<LogEventProperty>());

            var textFormatterMock = new Mock<ITextFormatter>(MockBehavior.Strict);
            textFormatterMock.Setup(s => s.Format(@event, It.IsAny<TextWriter>()))
                .Callback((LogEvent l, TextWriter t) => { l.RenderMessage(t); });

            var transformedEvent = transformer.TransformEvent(textFormatterMock.Object, @event);

            Assert.Equal(eventDateTime.UtcDateTime, transformedEvent.Timestamp);
            Assert.Equal(@event.MessageTemplate.Text, transformedEvent.Message);
            textFormatterMock.VerifyAll();
        }

        [Fact(DisplayName = "TransformEvent - Large message truncated")]
        public void LargeMessage()
        {
            var largeEventMessage = CloudWatchLogsSinkTests.CreateMessage(CloudWatchLogSink.MaxLogEventSize + 1, Alphabet);

            var eventDateTime = DateTimeOffset.Now;
            var transformer = new DefaultEventTransformer();
            var @event = new LogEvent(eventDateTime, LogEventLevel.Information, null, new MessageTemplateParser().Parse(largeEventMessage), Enumerable.Empty<LogEventProperty>());

            var textFormatterMock = new Mock<ITextFormatter>(MockBehavior.Strict);
            textFormatterMock.Setup(s => s.Format(@event, It.IsAny<TextWriter>()))
                .Callback((LogEvent l, TextWriter t) => { l.RenderMessage(t); });

            var transformedEvent = transformer.TransformEvent(textFormatterMock.Object, @event);

            Assert.Equal(eventDateTime.UtcDateTime, transformedEvent.Timestamp);
            Assert.Equal(CloudWatchLogSink.MaxLogEventSize, Encoding.UTF8.GetByteCount(transformedEvent.Message));
            Assert.Equal(largeEventMessage.Substring(0, transformedEvent.Message.Length), transformedEvent.Message);
        }
    }
}
