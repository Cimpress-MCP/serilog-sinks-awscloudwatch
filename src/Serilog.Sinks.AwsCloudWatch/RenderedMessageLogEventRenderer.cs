using System.IO;
using Serilog.Events;

namespace Serilog.Sinks.AwsCloudWatch
{
    public class RenderedMessageLogEventRenderer : ILogEventRenderer
    {
        public string RenderLogEvent(LogEvent logEvent)
        {
            using (var writer = new StringWriter())
            {
                logEvent.RenderMessage(writer);
                return writer.ToString();
            }
        }
    }
}