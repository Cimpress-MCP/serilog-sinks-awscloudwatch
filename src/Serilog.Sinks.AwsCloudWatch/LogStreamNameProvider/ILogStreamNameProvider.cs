namespace Serilog.Sinks.AwsCloudWatch
{
    public interface ILogStreamNameProvider
    {
        /// <summary>
        /// Gets the log stream name.
        /// </summary>
        /// <returns></returns>
        string GetLogStreamName();
    }
}