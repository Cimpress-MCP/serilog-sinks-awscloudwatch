using System;

namespace Serilog.Sinks.AwsCloudWatch
{
    internal class ConfigurationException : Exception
    {
        public ConfigurationException(string message) : base(message) {}
    }
}