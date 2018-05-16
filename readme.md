# Serilog Sink for AWS CloudWatch

This Serilog Sink allows to log to [AWS CloudWatch](https://aws.amazon.com/cloudwatch/). It supports .NET Framework 4.5 and .NET Core.

## Version and build status

[![NuGet version](https://badge.fury.io/nu/Serilog.Sinks.AwsCloudWatch.svg)](https://badge.fury.io/nu/Serilog.Sinks.AwsCloudWatch)

![Build status](https://ci.appveyor.com/api/projects/status/github/Cimpress-MCP/serilog-sinks-awscloudwatch?branch=master&svg=true)

## Usage

```cs
// name of the log group
var logGroupName = "myLogGroup/" + env.EnvironmentName;

// customer renderer (optional, defaults to a simple rendered message of Serilog's LogEvent
var renderer = new MyCustomRenderer();

// options for the sink defaults in https://github.com/Cimpress-MCP/serilog-sinks-awscloudwatch/blob/master/src/Serilog.Sinks.AwsCloudWatch/CloudWatchSinkOptions.cs
CloudWatchSinkOptions options = new CloudWatchSinkOptions
{
  LogGroupName = logGroupName,

  // Pick on of the following
  LogEventRenderer = MyCustomRenderer,
  TextFormatter = MyCustomTextFormatter,
  
  // other defaults defaults
  MinimumLogEventLevel = LogEventLevel.Information,
  BatchSizeLimit = 100,
  Period = TimeSpan.FromSeconds(10),
  CreateLogGroup = true,
  LogStreamNameProvider = new DefaultLogStreamProvider(),
  RetryAttempts = 5
};

// setup AWS CloudWatch client
AWSCredentials credentials = new BasicAWSCredentials(myAwsAccessKey, myAwsSecretKey);
IAmazonCloudWatchLogs client = new AmazonCloudWatchLogsClient(credentials, myAwsRegion);

// Attach the sink to the logger configuration
Log.Logger = new LoggerConfiguration().MinimumLevel.Information()
  .WriteTo.AmazonCloudWatch(options, client)
  .CreateLogger();
```

## Troubleshooting

Errors related to the setup of the Sink (for example, invalid AWS credentials), or problems during sending the data are logged to [Serilog's SelfLog](https://github.com/serilog/serilog/wiki/Debugging-and-Diagnostics).

Short version, enable it with something like the following command:

```cs
Serilog.Debugging.SelfLog.Enable(Console.Error);
```

## Contribution

We value your input as part of direct feedback to us, by filing issues, or preferably by directly contributing improvements:

1. Fork this repository
1. Create a branch
1. Contribute
1. Pull request
