using System.IO;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.AwsCloudWatch
{
    /// <summary>
    /// An <see cref="ILogEventRenderer"/> that simply calls the LogEvent's RenderMessage function.
    /// </summary>
    public class RenderedMessageLogEventRenderer : ILogEventRenderer
    {
        private readonly ITextFormatter formatter;

        public RenderedMessageLogEventRenderer()
            : this(new PlainTextFormatter())
        {

        }

        public RenderedMessageLogEventRenderer(ITextFormatter formatter)
        {
            this.formatter = formatter;
        }

        public string RenderLogEvent(LogEvent logEvent)
        {
            using (var writer = new StringWriter())
            {
                this.formatter.Format(logEvent, writer);
                return writer.ToString();
            }
        }
    }

    internal sealed class PlainTextFormatter : ITextFormatter
    {
        public void Format(LogEvent logEvent, TextWriter output)
        {
            logEvent.RenderMessage(output);
        }
    }
}
