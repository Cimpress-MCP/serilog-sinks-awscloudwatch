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
    /// Poperly handles truncating of multi-byte characters (UTF-8) at the small performance cost. Use <see cref="DefaultEventTransformer"/> if you're 
    /// confident that your message will only consist of ASCII characters or it will never exceeds maximum length.
    /// </summary>
    public class UnicodeEventTransformer : IEventTransformer
    {
        /// <summary>
        /// Performs message transformation and multibyte-aware truncation.
        /// </summary>
        /// <param name="textFormatter">TextFormatter to be used</param>
        /// <param name="event">Serilog event</param>
        /// <returns>Cloudwatch event object</returns>
        public InputLogEvent TransformEvent(ITextFormatter textFormatter, LogEvent @event)
        {
            char[] message;
            using (var writer = new StringWriter())
            {
                textFormatter.Format(@event, writer);
                writer.Flush();
                var sb = writer.GetStringBuilder();
                message = new char[sb.Length];
                sb.CopyTo(0, message, 0, message.Length);
            }

            var messageLength = message.Length;
            if (Encoding.UTF8.GetByteCount(message) > CloudWatchLogSink.MaxLogEventSize)
            {
                // truncate event message
                Debugging.SelfLog.WriteLine("Truncating log event with length of {0}", messageLength);
                messageLength = GetMaximumMessageLength(message);
            }
            return new InputLogEvent
            {
                Message = new String(message, 0, messageLength),
                Timestamp = @event.Timestamp.UtcDateTime
            };
        }

        private static int GetMaximumMessageLength(char[] message)
        {
            var proposedLength = message.Length;
            int bytesDelta = CloudWatchLogSink.MaxLogEventSize - Encoding.UTF8.GetByteCount(message, 0, proposedLength);
            while (bytesDelta < 0)
            {
                proposedLength += Math.Min(bytesDelta / 4, -1); // Maximum UTF8 char size is 32 bits
                bytesDelta = CloudWatchLogSink.MaxLogEventSize - Encoding.UTF8.GetByteCount(message, 0, proposedLength);
            }
            return proposedLength;
        }
    }
}
