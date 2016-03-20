using System.IO;
using Serilog.Events;

namespace Serilog.Sinks.AwsCloudWatch
{
    /// <summary>
    /// An <see cref="ILogEventRenderer"/> that simply calls the LogEvent's RenderMessage function.
    /// </summary>
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