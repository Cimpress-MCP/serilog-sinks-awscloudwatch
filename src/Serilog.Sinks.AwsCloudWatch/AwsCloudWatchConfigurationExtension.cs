using System;
using Amazon;
using Amazon.CloudWatchLogs;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.AwsCloudWatch
{
    public static class AwsCloudWatchConfigurationExtension
    {
        /// <summary>
        /// Activates logging to AWS CloudWatch
        /// </summary>
        /// <param name="loggerConfiguration">The LoggerSinkConfiguration to register this sink with.</param>
        /// <param name="options">Options to be used for the CloudWatch sink. <see cref="CloudWatchSinkOptions"/> for details.</param>
        /// <param name="cloudWatchClient">An AWS CloudWatch client which includes access to AWS and AWS specific settings like the AWS region.</param>
        /// <returns></returns>
        public static LoggerConfiguration AmazonCloudWatch(this LoggerSinkConfiguration loggerConfiguration, CloudWatchSinkOptions options, IAmazonCloudWatchLogs cloudWatchClient)
        {
            // validating input parameters
            if (loggerConfiguration == null) throw new ArgumentNullException(nameof(loggerConfiguration));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(options.LogGroupName)) throw new ArgumentException("options.LogGroupName");
            if (cloudWatchClient == null) throw new ArgumentNullException(nameof(cloudWatchClient));

            // create the sink
            ILogEventSink sink = new CloudWatchLogSink(cloudWatchClient, options);

            // register the sink
            return loggerConfiguration.Sink(sink, options.MinimumLogEventLevel);
        }

        /// <summary>
        /// Activates logging to AWS CloudWatch
        /// </summary>
        /// <remarks>This overload is intended to be used via AppSettings integration.</remarks>
        /// <param name="loggerConfiguration">The LoggerSinkConfiguration to register this sink with.</param>
        /// <param name="logGroupName">The log group name to be used in AWS CloudWatch.</param>
        /// <param name="cloudWatchClient">An AWS CloudWatch client which includes access to AWS and AWS specific settings like the AWS region.</param>
        /// <param name="logEventRenderer">A renderer to render Serilog's LogEvent.</param>
        /// <param name="minimumLogEventLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="batchSizeLimit">The batch size to be used when uploading logs to AWS CloudWatch.</param>
        /// <param name="period">The period to be used when a batch upload should be triggered.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="logGroupName"/> is <see langword="null"/>.</exception>
        public static LoggerConfiguration AmazonCloudWatch(
            this LoggerSinkConfiguration loggerConfiguration,
            string logGroupName,
            IAmazonCloudWatchLogs cloudWatchClient = null,
            ILogEventRenderer logEventRenderer = null,
            LogEventLevel minimumLogEventLevel = CloudWatchSinkOptions.DefaultMinimumLogEventLevel,
            int batchSizeLimit = CloudWatchSinkOptions.DefaultBatchSizeLimit,
            TimeSpan? period = null)
        {
            if (logGroupName == null) throw new ArgumentNullException(nameof(logGroupName));

            var options = new CloudWatchSinkOptions
            {
                LogGroupName = logGroupName,
                MinimumLogEventLevel = minimumLogEventLevel,
                BatchSizeLimit = batchSizeLimit,
                Period = period ?? CloudWatchSinkOptions.DefaultPeriod,
                LogEventRenderer = logEventRenderer
            };
            var client = cloudWatchClient ?? new AmazonCloudWatchLogsClient();
            return loggerConfiguration.AmazonCloudWatch(options, client);
        }

        /// <summary>
        /// Activates logging to AWS CloudWatch
        /// </summary>
        /// <remarks>This overload is intended to be used via AppSettings integration.</remarks>
        /// <param name="loggerConfiguration">The LoggerSinkConfiguration to register this sink with.</param>
        /// <param name="logGroupName">The log group name to be used in AWS CloudWatch.</param>
        /// <param name="regionName">The system name of the region to which to write.</param>
        /// <param name="logEventRenderer">A renderer to render Serilog's LogEvent.</param>
        /// <param name="minimumLogEventLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="batchSizeLimit">The batch size to be used when uploading logs to AWS CloudWatch.</param>
        /// <param name="period">The period to be used when a batch upload should be triggered.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="logGroupName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="regionName"/> is <see langword="null"/>.</exception>
        public static LoggerConfiguration AmazonCloudWatch(
            this LoggerSinkConfiguration loggerConfiguration,
            string logGroupName,
            string regionName,
            ILogEventRenderer logEventRenderer = null,
            LogEventLevel minimumLogEventLevel = CloudWatchSinkOptions.DefaultMinimumLogEventLevel,
            int batchSizeLimit = CloudWatchSinkOptions.DefaultBatchSizeLimit,
            TimeSpan? period = null)
        {
            if (logGroupName == null) throw new ArgumentNullException(nameof(logGroupName));
            if (regionName == null) throw new ArgumentNullException(nameof(regionName));

            var options = new CloudWatchSinkOptions
            {
                LogGroupName = logGroupName,
                MinimumLogEventLevel = minimumLogEventLevel,
                BatchSizeLimit = batchSizeLimit,
                Period = period ?? CloudWatchSinkOptions.DefaultPeriod,
                LogEventRenderer = logEventRenderer
            };
            var client = new AmazonCloudWatchLogsClient(RegionEndpoint.GetBySystemName(regionName));
            return loggerConfiguration.AmazonCloudWatch(options, client);
        }

        /// <summary>
        /// Activates logging to AWS CloudWatch
        /// </summary>
        /// <remarks>This overload is intended to be used via AppSettings integration.</remarks>
        /// <param name="loggerConfiguration">The LoggerSinkConfiguration to register this sink with.</param>
        /// <param name="logGroupName">The log group name to be used in AWS CloudWatch.</param>
        /// <param name="accessKey">The access key to use to access AWS CloudWatch.</param>
        /// <param name="secretAccessKey">The secret access key to use to access AWS CloudWatch.</param>
        /// <param name="regionName">The system name of the region to which to write.</param>
        /// <param name="logEventRenderer">A renderer to render Serilog's LogEvent.</param>
        /// <param name="minimumLogEventLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="batchSizeLimit">The batch size to be used when uploading logs to AWS CloudWatch.</param>
        /// <param name="period">The period to be used when a batch upload should be triggered.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="logGroupName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="accessKey"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="secretAccessKey"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="regionName"/> is <see langword="null"/>.</exception>
        public static LoggerConfiguration AmazonCloudWatch(
            this LoggerSinkConfiguration loggerConfiguration,
            string logGroupName,
            string accessKey,
            string secretAccessKey,
            string regionName,
            ILogEventRenderer logEventRenderer = null,
            LogEventLevel minimumLogEventLevel = CloudWatchSinkOptions.DefaultMinimumLogEventLevel,
            int batchSizeLimit = CloudWatchSinkOptions.DefaultBatchSizeLimit,
            TimeSpan? period = null)
        {
            if (logGroupName == null) throw new ArgumentNullException(nameof(logGroupName));
            if (accessKey == null) throw new ArgumentNullException(nameof(accessKey));
            if (secretAccessKey == null) throw new ArgumentNullException(nameof(secretAccessKey));
            if (regionName == null) throw new ArgumentNullException(nameof(regionName));

            var options = new CloudWatchSinkOptions
            {
                LogGroupName = logGroupName,
                MinimumLogEventLevel = minimumLogEventLevel,
                BatchSizeLimit = batchSizeLimit,
                Period = period ?? CloudWatchSinkOptions.DefaultPeriod,
                LogEventRenderer = logEventRenderer
            };
            var client = new AmazonCloudWatchLogsClient(accessKey, secretAccessKey,
                RegionEndpoint.GetBySystemName(regionName));
            return loggerConfiguration.AmazonCloudWatch(options, client);
        }
    }
}
