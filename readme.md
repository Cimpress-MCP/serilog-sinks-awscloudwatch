# Serilog Sink for AWS CloudWatch

This Serilog Sink allows to log to [AWS CloudWatch](https://aws.amazon.com/cloudwatch/). It supports .NET Framework 4.6, DNX451 and .NET Core.

## Version and build status

[![NuGet version](https://badge.fury.io/nu/Serilog.Sinks.AwsCloudWatch.svg)](https://badge.fury.io/nu/Serilog.Sinks.AwsCloudWatch)

![Build status](https://ci.appveyor.com/api/projects/status/github/Cimpress-MCP/serilog-sinks-awscloudwatch?branch=master&svg=true)

## Usage

```cs
// name of the log group
var logGroupName = "myLogGroup/" + env.EnvironmentName;

// customer renderer (optional, defaults to a simple rendered message of Serilog's LogEvent
var renderer = new MyCustomRenderer();

// options for the sink, specifying the log group name
CloudWatchSinkOptions options = new CloudWatchSinkOptions {LogGroupName = logGroupName, LogEventRenderer = MyCustomRenderer};

// setup AWS CloudWatch client
AWSCredentials credentials = new BasicAWSCredentials(myAwsAccessKey, myAwsSecretKey);
IAmazonCloudWatchLogs client = new AmazonCloudWatchLogsClient(credentials, myAwsRegion);

// Attach the sink to the logger configuration
Log.Logger = new LoggerConfiguration().MinimumLevel.Information()
  .WriteTo.AmazonCloudWatch(options, client)
  .CreateLogger();
```

## Contribution

We value your input as part of direct feedback to us, by filing issues, or preferably by directly contributing improvements:

1. Fork this repository
1. Create a branch
1. Contribute
1. Pull request
