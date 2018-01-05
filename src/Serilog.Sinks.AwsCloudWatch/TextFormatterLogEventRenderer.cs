using System;
using System.IO;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.AwsCloudWatch
{
    /// <summary>
    /// An <see cref="ILogEventRenderer"/> that wraps a Serilog <see cref="ITextFormatter"/> and uses it to format the log output
    /// </summary>
    public class TextFormatterLogEventRenderer : ILogEventRenderer
    {
        private readonly ITextFormatter textFormatter;

        public TextFormatterLogEventRenderer(ITextFormatter textFormatter)
        {
            this.textFormatter = textFormatter ?? throw new ArgumentNullException(nameof(textFormatter));
        }

        public string RenderLogEvent(LogEvent logEvent)
        {
            using (var writer = new StringWriter())
            {
                this.textFormatter.Format(logEvent, writer);
                writer.Flush();
                return writer.ToString();
            }
        }
    }
}