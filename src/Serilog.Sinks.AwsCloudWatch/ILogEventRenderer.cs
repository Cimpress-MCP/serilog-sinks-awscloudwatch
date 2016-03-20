using Serilog.Events;

namespace Serilog.Sinks.AwsCloudWatch
{
    /// <summary>
    /// A renderer that converts a Serilog log event to a string, which will be published to AWS CloudWatch.
    /// </summary>
    public interface ILogEventRenderer
    {
        /// <summary>
        /// Converts a log event to a string that can be sent to AWS CloudWatch. If JSON or other custom data formats should be logged,
        /// they need to be converted to a string by the implementing renderer.
        /// </summary>
        /// <param name="logEvent">The log event to render.</param>
        /// <returns>A string representation of the log event.</returns>
        string RenderLogEvent(LogEvent logEvent);
    }
}