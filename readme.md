# Serilog Sink for AWS CloudWatch

This Serilog Sink allows to log to [AWS CloudWatch](https://aws.amazon.com/cloudwatch/). It supports .NET Core (.NET Framework 4.5 is deprecated and no longer supported)

## Version and build status

[![NuGet version](https://badge.fury.io/nu/Serilog.Sinks.AwsCloudWatch.svg)](https://badge.fury.io/nu/Serilog.Sinks.AwsCloudWatch) ![Build status](https://ci.appveyor.com/api/projects/status/github/Cimpress-MCP/serilog-sinks-awscloudwatch?branch=master&svg=true)

## Usage
There are two important aspects for configuring this library.  The first is providing the configuration options necessary via the [`ICloudWatchSinkOptions` implementation](#CloudWatchSinkOptions).  And the second is [configuring the AWS Credentials](#Configuring-Credentials).  Both of these are required to log to CloudWatch.

### CloudWatchSinkOptions
This library provides an extension method which takes in only a `ICloudWatchSinkOptions` instance and the `IAmazonCloudWatchLogs` instance.

##### Configuration via Code First
The preferred approach for configuration is to construct the necessary objects via code and pass them directly to the library extension method.
``` cs
  // name of the log group
  var logGroupName = "myLogGroup/" + env.EnvironmentName;

  // customer formatter
  var formatter = new MyCustomTextFormatter();

  // options for the sink defaults in https://github.com/Cimpress-MCP/serilog-sinks-awscloudwatch/blob/master/src/Serilog.Sinks.AwsCloudWatch/CloudWatchSinkOptions.cs
  var options = new CloudWatchSinkOptions
  {
    // the name of the CloudWatch Log group for logging
    LogGroupName = logGroupName,

    // the main formatter of the log event
    TextFormatter = formatter,
    
    // other defaults defaults
    MinimumLogEventLevel = LogEventLevel.Information,
    BatchSizeLimit = 100,
    QueueSizeLimit = 10000,
    Period = TimeSpan.FromSeconds(10),
    CreateLogGroup = true,
    LogStreamNameProvider = new DefaultLogStreamProvider(),
    RetryAttempts = 5
  };

  // setup AWS CloudWatch client
  var client = new AmazonCloudWatchLogsClient(myAwsRegion);

  // Attach the sink to the logger configuration
  Log.Logger = new LoggerConfiguration()
    .WriteTo.AmazonCloudWatch(options, client)
    .CreateLogger();
```
  
##### Configuration via Fluent Code First 
 Call the extension method passing the configuration values that you wish to make use of.
``` cs
  // setup AWS CloudWatch client
  var client = myAppConfigRoot.GetAWSOptions().CreateServiceClient<IAmazonCloudWatchLogs>();

  // Attach the sink to the logger configuration
  Log.Logger = new LoggerConfiguration()
    .WriteTo.AmazonCloudWatch(
		"myLogGroup/" + env.EnvironmentName, 
		batchSizeLimit = 100,
		queueSizeLimit = 10000,
		batchUploadPeriodInSeconds = 15,
		createLogGroup = true,
		maxRetryAttempts = 3
		cloudWatchClient = client)
    .CreateLogger();
```

##### Configuration via config file
While not recommended, it is still possible to config the library via a configuration file.  There are two libraries which provide these capabilities.

* [Serilog.Settings.AppSettings](https://github.com/serilog/serilog-settings-appsettings)
Configuration is done by `App.config` or `Web.config` file.  Create a concrete implementation of the `ICloudWatchSinkOptions` interface, and specify the necessary configuration values.  Then in the configuration file, specify the following:

``` xml
  <!-- {Assembly} name is `typeof(YourOptionsClass).AssemblyQualifiedName` and {Namespace} is the class namespace. -->
  <add key="serilog:write-to:AmazonCloudWatch.options" value="{namespace}.CloudWatchSinkOptions, {assembly}, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" />
```

* [Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration)
Configuration is done by an `appsettings.json` file (or optional specific override.  Create a concrete implementation of the `ICloudWatchSinkOptions` interface, and specify the necessary configuration values.  Then in the configuration file, specify the following:

``` js
  // {Assembly} name is `typeof(YourOptionsClass).AssemblyQualifiedName` and {Namespace} is the class namespace.
  {
    "Args": {
      "options": "{namespace}.CloudWatchSinkOptions, {assembly}"
    }
  }
```
  
  Alternatively you may configure the library without creating a concrete instance of the `ICloudWatchSinkOptions` interface however this will cause the AWS Service client to follow the credential rules in the [official AWS SDK documentation](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-config-creds.html). You may configure any of the passed values in the Extension method. 
  
``` json
  {
    "Serilog": {
        "Using": [ "Serilog.Sinks.AwsCloudWatch" ],
        "MinimumLevel": "Verbose",
        "WriteTo": [            
            {
                "Name": "AmazonCloudWatch",
                "Args": {
                    "logGroup": "your-app",
                    "logStreamPrefix": "environment/component",
                    "restrictedToMinimumLevel": "Verbose"
                }
            }
        ]
    }
  }
```

or using XML:

``` xml
  <add key="serilog:using:AwsCloudWatch" value="Serilog.Sinks.AwsCloudWatch" />
  <add key="serilog:write-to:AmazonCloudWatch.logGroup" value="your-app" />
  <add key="serilog:write-to:AmazonCloudWatch.logStreamPrefix" value="environment/component" />
  <add key="serilog:write-to:AmazonCloudWatch.restrictedToMinimumLevel" value="Verbose" />
```

## Configuring Credentials
AmazonCloudWatchLogsClient from the AWS SDK requires AWS credentials.  To correctly associate credentials with the library, please refer to [The Official AWS Recommendation on C#](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-config-creds.html) for credentials management.  To reiterate here:
* Preferred => Use IAM Profile set on your instance, machine, lambda function.
* Create a credentials profile with your AWS credentials.
* Use Environment Variables
* Manually construct the credentials via:
``` cs
  var options = new CredentialProfileOptions { AccessKey = "access_key", SecretKey = "secret_key" };
  var profile = new Amazon.Runtime.CredentialManagement.CredentialProfile("basic_profile", options);
  profile.Region = GetBySystemName("eu-west-1"); // OR RegionEndpoint.EUWest1
  var netSDKFile = new NetSDKCredentialsFile();
  netSDKFile.RegisterProfile(profile);
```

## Troubleshooting
* `Cannot find region in config` or `Cannot find credentials in config`
AWS configuration is not complete.  Refer to the [Configuring Credentials](#Configuring-Credentials) section to complete the configuration.

* Errors related to the setup of the Sink (for example, invalid AWS credentials), or problems during sending the data are logged to [Serilog's SelfLog](https://github.com/serilog/serilog/wiki/Debugging-and-Diagnostics).
Short version, enable it with something like the following command:

``` cs
  Serilog.Debugging.SelfLog.Enable(Console.Error);
```

## Contribution

We value your input as part of direct feedback to us, by filing issues, or preferably by directly contributing improvements:

1. Fork this repository
1. Create a branch
1. Contribute
1. Pull request
