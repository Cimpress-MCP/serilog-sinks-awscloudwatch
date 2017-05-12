using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.AwsCloudWatch
{
    public class DefaultLogStreamProvider : ILogStreamNameProvider
    {
        private readonly string DATETIME_FORMAT = "yyyy-MM-dd-hh-mm-ss";

        public DefaultLogStreamProvider() { }

        public string GetLogStreamName()
        {
            return $"{DateTime.UtcNow.ToString(DATETIME_FORMAT)}_{Dns.GetHostName()}_{Guid.NewGuid()}";
        }
    }
}
