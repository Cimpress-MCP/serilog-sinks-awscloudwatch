using Amazon.CloudWatchLogs;
using Amazon.Runtime;
using Serilog;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.AwsCloudWatch;
using System;
using System.IO;

namespace SerilogCloudWatch
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var awsCredentials = new BasicAWSCredentials("AWS AccessKey", "AWD SecretKey");
            var region = Amazon.RegionEndpoint.APSoutheast2;

            var outputTemplate = new AwsTextFormatter();
            var logStream = new AwsLogStream();
            var logGroupName = "AWSGroupName";

            // for debugging purposes, disable when it goes to production
            Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine(msg));

            var options = new CloudWatchSinkOptions()
            {
                LogGroupName = logGroupName,
                TextFormatter = outputTemplate,
                MinimumLogEventLevel = LogEventLevel.Verbose,
                BatchSizeLimit = 1000,
                QueueSizeLimit = 10000,
                Period = TimeSpan.FromSeconds(5),
                CreateLogGroup = true,
                LogStreamNameProvider = new DefaultLogStreamProvider(), // or you can use logStream variable defined if you need a custom name
                RetryAttempts = 5
            };

            var client = new AmazonCloudWatchLogsClient(awsCredentials, region);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.AmazonCloudWatch(options, client)
                .CreateLogger();

            Log.Information($"Hello, Info");
            Log.Warning($"Hello, Warn");
            Log.Debug($"Hello, Debug");
            Log.Error($"Hello, Error");
            Log.Fatal($"Hello, Fatal");

            Log.CloseAndFlush();

            Console.ReadKey();
        }
    }

    public class AwsTextFormatter : ITextFormatter
    {
        public void Format(LogEvent logEvent, TextWriter output)
        {
            output.Write("Timestamp - {0} | Level - {1} | Message {2} {3}", logEvent.Timestamp, logEvent.Level,
                logEvent.MessageTemplate, output.NewLine);
            if (logEvent.Exception != null)
            {
                output.Write("Exception - {0}", logEvent.Exception);
            }
        }
    }

    public class AwsLogStream : ILogStreamNameProvider
    {
        public string GetLogStreamName()
        {
            return "Test";
        }
    }
}