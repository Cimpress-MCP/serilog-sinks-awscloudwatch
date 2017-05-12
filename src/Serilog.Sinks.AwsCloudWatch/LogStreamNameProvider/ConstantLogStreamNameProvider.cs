using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.AwsCloudWatch
{
    public class ConstantLogStreamNameProvider : ILogStreamNameProvider
    {
        private string _prefix = string.Empty;

        public ConstantLogStreamNameProvider(string prefix)
        {
            this._prefix = prefix;
        }

        public string GetLogStreamName()
        {
            return $"{_prefix}_{Guid.NewGuid()}";
        }
    }
}
