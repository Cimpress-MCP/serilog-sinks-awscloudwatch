using System;
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
        /// <param name="loggerConfiguration">The LoggerSinkConfiguration to register this sink with.</param>
        /// <param name="logGroupName">The log group name you want to use.</param>
        /// <param name="accessKey">AWS Access Key ID.</param>
        /// <param name="secretKey">AWS Secret Access Key.</param>
        /// <param name="region">The region to connect.</param>
        /// <returns></returns>
        public static LoggerConfiguration AmazonCloudWatch(this LoggerSinkConfiguration loggerConfiguration, string logGroupName, string accessKey, string secretKey, string region)
        {
            // validating input parameters
            if (loggerConfiguration == null) throw new ArgumentNullException(nameof(loggerConfiguration));
            if (string.IsNullOrEmpty(logGroupName)) throw new ArgumentException(nameof(logGroupName));
            if (string.IsNullOrEmpty(accessKey)) throw new ArgumentException(nameof(accessKey));
            if (string.IsNullOrEmpty(secretKey)) throw new ArgumentException(nameof(secretKey));
            if (string.IsNullOrEmpty(region)) throw new ArgumentException(nameof(region));

            CloudWatchSinkOptions options = new CloudWatchSinkOptions() { LogGroupName = logGroupName };
            IAmazonCloudWatchLogs client = new AmazonCloudWatchLogsClient(accessKey, secretKey, Amazon.RegionEndpoint.GetBySystemName(region));

            return loggerConfiguration.AmazonCloudWatch(options, client);
        }
    }
}
