using Serilog.Events;

namespace Serilog.Sinks.AwsCloudWatch
{
    public interface ILogEventRenderer
    {
        string RenderLogEvent(LogEvent logEvent);
    }
}