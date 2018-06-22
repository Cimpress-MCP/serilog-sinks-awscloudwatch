using System;
using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.Runtime;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

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
            var sink = new CloudWatchLogSink(cloudWatchClient, options);

            // register the sink
            return loggerConfiguration.Sink(sink, options.MinimumLogEventLevel);
        }

        /// <summary>
        /// Activates logging to AWS CloudWatch
        /// </summary>
        /// <remarks>This overload is intended to be used via AppSettings integration.</remarks>
        /// <param name="loggerConfiguration">The LoggerSinkConfiguration to register this sink with.</param>
        /// <param name="logGroupName">The log group name to be used in AWS CloudWatch.</param>
        /// <param name="formatter">A formatter to format Serilog's LogEvent.</param>
        /// <param name="logStreamNameProvider">The log stream name provider.</param>
        /// <param name="minimumLogEventLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="batchSizeLimit">The batch size to be used when uploading logs to AWS CloudWatch.</param>
        /// <param name="period">The period to be used when a batch upload should be triggered.</param>
        /// <param name="retryAttempts">The period to be used when a batch upload should be triggered.</param>
        /// <param name="createLogGroup">Automatically create the specify log group if it does not exist.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="logGroupName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="formatter"/> is <see langword="null"/>.</exception>
        public static LoggerConfiguration AmazonCloudWatch(
            this LoggerSinkConfiguration loggerConfiguration,
            string logGroupName,
            ITextFormatter formatter,
            ILogStreamNameProvider logStreamNameProvider = null,
            LogEventLevel minimumLogEventLevel = CloudWatchSinkOptions.DefaultMinimumLogEventLevel,
            int batchSizeLimit = CloudWatchSinkOptions.DefaultBatchSizeLimit,
            TimeSpan? period = null,
            byte retryAttempts = CloudWatchSinkOptions.DefaultRetryAttempts,
            bool createLogGroup = CloudWatchSinkOptions.DefaultCreateLogGroup)
        {
            if (logGroupName == null) throw new ArgumentNullException(nameof(logGroupName));
            if (logStreamNameProvider == null) { throw new ArgumentNullException(nameof(logStreamNameProvider)); }

            var options = new CloudWatchSinkOptions
            {
                LogGroupName = logGroupName,
                TextFormatter = formatter,
                LogStreamNameProvider = logStreamNameProvider ?? new DefaultLogStreamProvider(),
                MinimumLogEventLevel = minimumLogEventLevel,
                BatchSizeLimit = batchSizeLimit,
                Period = period ?? CloudWatchSinkOptions.DefaultPeriod,
                RetryAttempts = retryAttempts,
                CreateLogGroup = createLogGroup
            };

            var client = new AmazonCloudWatchLogsClient();
            return loggerConfiguration.AmazonCloudWatch(options, client);
        }
    }
}
