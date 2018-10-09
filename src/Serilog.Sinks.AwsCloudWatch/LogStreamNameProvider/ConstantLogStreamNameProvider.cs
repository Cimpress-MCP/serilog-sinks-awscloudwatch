using System;

namespace Serilog.Sinks.AwsCloudWatch
{
    public class ConstantLogStreamNameProvider : ILogStreamNameProvider
    {
        private string _prefix = string.Empty;

        public ConstantLogStreamNameProvider(string prefix)
        {
            _prefix = prefix;
        }

        public string GetLogStreamName()
        {
            return $"{_prefix}_{Guid.NewGuid()}";
        }

        public bool IsUniqueName()
        {
            return true;
        }
    }
}