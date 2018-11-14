using Serilog.Events;

namespace Serilog.Sinks.AwsCloudWatch
{
    public static class LogEventExtensions
    {
        public static void EnrichFromOptions(this LogEvent logEvent, ICloudWatchSinkOptions options)
        {
            if (options.RenderMessageTemplate)
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty(options.RenderMessageTemplatePropertyName, new ScalarValue(logEvent.RenderMessage())));
            }
        }
    }
}