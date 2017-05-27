using Serilog.Events;
using System.IO;

namespace Serilog.Sinks.AwsCloudWatch
{
    public class DefaultLogEventRenderer : ILogEventRenderer
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