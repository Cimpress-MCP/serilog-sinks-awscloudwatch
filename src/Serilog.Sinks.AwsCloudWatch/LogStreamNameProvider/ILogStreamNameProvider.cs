namespace Serilog.Sinks.AwsCloudWatch
{
    public interface ILogStreamNameProvider
    {
        /// <summary>
        /// Gets the log stream name.
        /// </summary>
        /// <returns></returns>
        string GetLogStreamName();

        /// <summary>
        /// Whether the name supports always creating a new log stream.  If not, a log stream will be reused when it exists.
        /// Note: appending to the same log stream across multiple processes may have unpredictable results
        /// </summary>
        bool IsUniqueName();
    }
}