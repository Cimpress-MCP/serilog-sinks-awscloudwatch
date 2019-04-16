using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Amazon.CloudWatchLogs.Model;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.AwsCloudWatch.EventTransformer
{
    /// <summary>
    /// Default implementation of IEventTransformer, generates Cloudwatch object and handles maximum Cloudwatch event size truncating message if needed.
    /// Does not handle truncating properly if multi-byte characters are present (UTF-8) - if this is your case, use <see cref="UnicodeEventTransformer"/> instead.
    /// </summary>
    public class DefaultEventTransformer : IEventTransformer
    {
        /// <summary>
        /// Performs message transformation and truncation.
        /// </summary>
        /// <param name="textFormatter">TextFormatter to be used</param>
        /// <param name="event">Serilog event</param>
        /// <returns>Cloudwatch event object</returns>
        public InputLogEvent TransformEvent(ITextFormatter textFormatter, LogEvent @event)
        {
            string message = null;
            using (var writer = new StringWriter())
            {
                textFormatter.Format(@event, writer);
                writer.Flush();
                message = writer.ToString();
            }
            var messageLength = System.Text.Encoding.UTF8.GetByteCount(message);
            if (messageLength > CloudWatchLogSink.MaxLogEventSize)
            {
                // truncate event message
                Debugging.SelfLog.WriteLine("Truncating log event with length of {0}", messageLength);
                message = message.Substring(0, CloudWatchLogSink.MaxLogEventSize);
            }
            return new InputLogEvent
            {
                Message = message,
                Timestamp = @event.Timestamp.UtcDateTime
            };

        }
    }
}
