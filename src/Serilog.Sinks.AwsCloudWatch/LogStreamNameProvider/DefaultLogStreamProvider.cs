using System;
using System.Net;

namespace Serilog.Sinks.AwsCloudWatch
{
    public class DefaultLogStreamProvider : ILogStreamNameProvider
    {
        private readonly string DATETIME_FORMAT = "yyyy-MM-dd-hh-mm-ss";

        public string GetLogStreamName()
        {
            return $"{DateTime.UtcNow.ToString(DATETIME_FORMAT)}_{Dns.GetHostName()}_{Guid.NewGuid()}";
        }
    }
}