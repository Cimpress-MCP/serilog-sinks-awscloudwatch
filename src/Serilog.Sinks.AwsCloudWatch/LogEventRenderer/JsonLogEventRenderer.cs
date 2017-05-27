using Serilog.Events;
using System.IO;
using Newtonsoft.Json;

namespace Serilog.Sinks.AwsCloudWatch
{
    public class JsonLogEventRenderer : ILogEventRenderer
    {
        public string RenderLogEvent(LogEvent logEvent)
        {
            return JsonConvert.SerializeObject(logEvent);
        }
    }
}
