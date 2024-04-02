using System;
using System.Net;
using Amazon.CloudWatchLogs;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Serilog.Sinks.AwsCloudWatch.LogStreamNameProvider;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.AwsCloudWatch
{
	/// <summary>Provides extensions for adding CloudWatch logging to your <see cref="Logger"/></summary>
    public static class AwsCloudWatchConfigurationExtension
    {
        /// <summary>
        /// Activates logging to AWS CloudWatch
        /// </summary>
        /// <param name="loggerConfiguration">The LoggerSinkConfiguration to register this sink with.</param>
        /// <param name="options">Options to be used for the CloudWatch sink. <see cref="ICloudWatchSinkOptions"/> and <see cref="CloudWatchSinkOptions"/> for details.</param>
        /// <param name="cloudWatchClient">An AWS CloudWatch client which includes access to AWS and AWS specific settings like the AWS region.</param>
        /// <returns></returns>
        public static LoggerConfiguration AmazonCloudWatch(this LoggerSinkConfiguration loggerConfiguration, ICloudWatchSinkOptions options, IAmazonCloudWatchLogs cloudWatchClient)
        {
            // validating input parameters
            if (loggerConfiguration == null)
            {
                throw new ArgumentNullException(nameof(loggerConfiguration));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (cloudWatchClient == null)
            {
                throw new ArgumentNullException(nameof(cloudWatchClient));
            }
            
            // the batched sink is 
            var batchedSink = new CloudWatchBatchedSink(cloudWatchClient, options);

            var sink = new PeriodicBatchingSink(batchedSink, new()
            {
	            BatchSizeLimit = options.BatchSizeLimit, 
	            Period = options.Period, 
	            QueueLimit = options.QueueSizeLimit
            });

            // register the sink
            return loggerConfiguration.Sink(sink, options.MinimumLogEventLevel);
        }


        /// <summary>
		/// Add an Amazon CloudWatch sink to your Serilog <see cref="ILogger"/> instance
		/// </summary>
		/// <param name="loggerConfiguration">The configuration we will add the sink to</param>
		/// <param name="logGroup">Name of the Log Group that we will write to (i.e. 'MyLogs')</param>
		/// <param name="logStreamPrefix">
		/// Prefix of the log stream that we will append to. We will default to writing to:
		/// <code>
		/// ${logStreamPrefix}/{HostName}/{UniqueInstanceGuid}
		/// </code>
		/// where you provide the log stream prefix, host name is looked up using <see cref="Dns.GetHostName" />
		/// and the Unique Instance Guid is generated at the time of log creation. This means you will have a
		/// separate log stream for each instance of your cloud watch logger. This is useful as it will
		/// generate a new log stream each time your program restarts. You can disable appending
		/// the guid by setting <paramref name="appendUniqueInstanceGuid"/> to false.
		/// </param>
		/// <param name="restrictedToMinimumLevel">Minimum log level to write to CloudWatch</param>
		/// <param name="batchSizeLimit">Maximum number of CloudWatch events we will try to write in each batch (default: 100)</param>
		/// <param name="createLogGroup">Should we attempt to create the log group if it doesn't already exist?</param>
		/// <param name="batchUploadPeriodInSeconds">Maximum length of time that we will hold log events in the queue before triggering a write to CloudWatch (default, 10 seconds)</param>
		/// <param name="queueSizeLimit">Maximum number of log events we can hold in the queue before triggering a send to CloudWatch (default 10,000)</param>
		/// <param name="maxRetryAttempts">Maximum number of retry attempts we will make to write to CloudWatch before failing</param>
		/// <param name="logGroupRetentionPolicy">Retention policy for your Log Group (Default: 1 week (7 days))</param>
		/// <param name="appendUniqueInstanceGuid">Should a unique guid be appended to the log stream that will be used for all writes from this log instance?</param>
		/// <param name="appendHostName">Should the machines HostName (as determined by <see cref="Dns.GetHostName" />) be appended to the log stream prefix?</param>
        /// <param name="textFormatter">The text formatter to use to format the logs (Defaults to <see cref="JsonFormatter"/> )</param>
		/// <param name="cloudWatchClient">
		/// Client to use to connect to AWS CloudWatch. Defaults to creating a new client which will follow the rules
		/// outlined in the <see href="https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-config-creds.html">AWS documentation</see>.
		/// </param>
		/// <returns><see cref="LoggerConfiguration"/> which can be used to fluently add more config details to your log</returns>
		public static LoggerConfiguration AmazonCloudWatch(
			this LoggerSinkConfiguration loggerConfiguration, 
			string logGroup,
			string logStreamPrefix,
			LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose,
			int batchSizeLimit = 100,
			int batchUploadPeriodInSeconds = 10,
			bool createLogGroup = true,
			int queueSizeLimit = 10000,
			byte maxRetryAttempts = 5,
			LogGroupRetentionPolicy logGroupRetentionPolicy = LogGroupRetentionPolicy.OneWeek,
			bool appendUniqueInstanceGuid = true,
			bool appendHostName = true,
			ITextFormatter textFormatter = null,
			IAmazonCloudWatchLogs cloudWatchClient = null)
        {
	        var provider = new ConfigurableLogStreamNameProvider(
								logStreamPrefix, 
								appendHostName, 
								appendUniqueInstanceGuid);

	        return AmazonCloudWatch(loggerConfiguration,
		        logGroup,
		        provider,
		        restrictedToMinimumLevel,
		        batchSizeLimit,
		        batchUploadPeriodInSeconds,
		        createLogGroup,
		        queueSizeLimit,
		        maxRetryAttempts,
		        logGroupRetentionPolicy,
		        textFormatter,
		        cloudWatchClient);
        }
        
        /// <summary>
		/// Add an Amazon CloudWatch sink to your Serilog <see cref="ILogger"/> instance
		/// </summary>
		/// <param name="loggerConfiguration">The configuration we will add the sink to</param>
		/// <param name="logGroup">Name of the Log Group that we will write to (i.e. 'MyLogs')</param>
        /// <param name="logStreamNameProvider">The log stream name provider to use to generate log stream names</param>
		/// <param name="restrictedToMinimumLevel">Minimum log level to write to CloudWatch</param>
		/// <param name="batchSizeLimit">Maximum number of CloudWatch events we will try to write in each batch (default: 100)</param>
		/// <param name="createLogGroup">Should we attempt to create the log group if it doesn't already exist?</param>
		/// <param name="batchUploadPeriodInSeconds">Maximum length of time that we will hold log events in the queue before triggering a write to CloudWatch (default, 10 seconds)</param>
		/// <param name="queueSizeLimit">Maximum number of log events we can hold in the queue before triggering a send to CloudWatch (default 10,000)</param>
		/// <param name="maxRetryAttempts">Maximum number of retry attempts we will make to write to CloudWatch before failing</param>
		/// <param name="logGroupRetentionPolicy">Retention policy for your Log Group (Default: 1 week (7 days))</param>
        /// <param name="textFormatter">The text formatter to use to format the logs (Defaults to <see cref="JsonFormatter"/> )</param>
        /// <param name="cloudWatchClient">
        /// Client to use to connect to AWS CloudWatch. Defaults to creating a new client which will follow the rules
        /// outlined in the <see href="https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-config-creds.html">AWS documentation</see>.
        /// </param>
		/// <returns><see cref="LoggerConfiguration"/> which can be used to fluently add more config details to your log</returns>
		public static LoggerConfiguration AmazonCloudWatch(
			this LoggerSinkConfiguration loggerConfiguration, 
			string logGroup,
			ILogStreamNameProvider logStreamNameProvider,
			LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose,
			int batchSizeLimit = 100,
			int batchUploadPeriodInSeconds = 10,
			bool createLogGroup = true,
			int queueSizeLimit = 10000,
			byte maxRetryAttempts = 5,
			LogGroupRetentionPolicy logGroupRetentionPolicy = LogGroupRetentionPolicy.OneWeek,
			ITextFormatter textFormatter = null,
			IAmazonCloudWatchLogs cloudWatchClient = null)
		{
			if (loggerConfiguration == null)
			{
				throw new ArgumentNullException(nameof(loggerConfiguration));
			}

			if (String.IsNullOrWhiteSpace(logGroup))
			{
				throw new ArgumentException("You must provide a log group name (like: 'your-application/your-component')", nameof(logGroup));
			}

			if (logStreamNameProvider == null)
			{
				throw new ArgumentNullException(nameof(logStreamNameProvider), "You must provide a log stream name provider (like DefaultLogStreamProvider)");
			}

			var options = new CloudWatchSinkOptions
			{
				BatchSizeLimit = batchSizeLimit,
				LogGroupName = logGroup,
				LogStreamNameProvider = logStreamNameProvider,
				CreateLogGroup = createLogGroup,
				MinimumLogEventLevel = restrictedToMinimumLevel,
				Period = TimeSpan.FromSeconds(batchUploadPeriodInSeconds),
				QueueSizeLimit = queueSizeLimit,
				RetryAttempts = maxRetryAttempts,
				LogGroupRetentionPolicy = logGroupRetentionPolicy,
				TextFormatter = textFormatter ?? new JsonFormatter()
			};

			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			var client = cloudWatchClient ?? new AmazonCloudWatchLogsClient();

			return AmazonCloudWatch(loggerConfiguration, options, client);
		}
    }
}
