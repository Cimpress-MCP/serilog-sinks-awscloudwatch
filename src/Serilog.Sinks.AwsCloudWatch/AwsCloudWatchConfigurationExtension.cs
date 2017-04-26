using System;
using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.Runtime;
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
        /// <param name="accessKey">The access key to use to access AWS CloudWatch.</param>
        /// <param name="secretAccessKey">The secret access key to use to access AWS CloudWatch.</param>
        /// <param name="regionName">The system name of the region to which to write.</param>
        /// <param name="logEventRenderer">A renderer to render Serilog's LogEvent.</param>
        /// <param name="minimumLogEventLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="batchSizeLimit">The batch size to be used when uploading logs to AWS CloudWatch.</param>
        /// <param name="period">The period to be used when a batch upload should be triggered.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="logGroupName"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// An invalid combination of <paramref name="accessKey"/>, <paramref name="secretAccessKey"/>,
        /// and <paramref name="regionName"/> has been provided.
        /// </exception>
        public static LoggerConfiguration AmazonCloudWatch(
            this LoggerSinkConfiguration loggerConfiguration,
            string logGroupName,
            string accessKey = null,
            string secretAccessKey = null,
            string regionName = null,
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
            var client = CreateClient(accessKey, secretAccessKey, regionName);
            return loggerConfiguration.AmazonCloudWatch(options, client);
        }

        /// <exception cref="InvalidOperationException">
        /// An invalid combination of <paramref name="accessKey"/>, <paramref name="secretAccessKey"/>,
        /// and <paramref name="regionName"/> has been provided.
        /// </exception>
        static IAmazonCloudWatchLogs CreateClient(
            string accessKey,
            string secretAccessKey,
            string regionName)
        {
            /* Some combinations of values are valid, but some are not.
             * 
             * accessKey | secretAccessKey | regionName | ->valid?
             * ---------------------------------------------------
             *    null   |       null      |    null    |    yes
             *    null   |       null      |   value    |    yes
             *    null   |      value      |    null    |     no
             *    null   |      value      |   value    |     no
             *   value   |       null      |    null    |     no
             *   value   |       null      |   value    |     no
             *   value   |      value      |    null    |    yes
             *   value   |      value      |   value    |    yes
             */

            if (regionName != null)
            {
                var region = RegionEndpoint.GetBySystemName(regionName);
                if (accessKey != null && secretAccessKey != null)
                {
                    var credentials = new BasicAWSCredentials(accessKey, secretAccessKey);
                    return new AmazonCloudWatchLogsClient(credentials, region);
                }
                return new AmazonCloudWatchLogsClient(region);
            }

            if (accessKey != null && secretAccessKey != null)
            {
                var credentials = new BasicAWSCredentials(accessKey, secretAccessKey);
                return new AmazonCloudWatchLogsClient(credentials);
            }

            if (accessKey == null && secretAccessKey == null)
            {
                return new AmazonCloudWatchLogsClient();
            }

            // If an invalid combination has been provided, yell.
            throw new InvalidOperationException(
                @"An invalid combination of ""accessKey"", ""secretAccessKey"", and ""regionName"" has been provided.");
        }
    }
}
