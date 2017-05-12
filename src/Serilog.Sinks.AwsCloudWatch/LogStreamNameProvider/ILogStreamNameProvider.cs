using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.AwsCloudWatch
{
    /// <summary>
    /// 
    /// </summary>
    public interface ILogStreamNameProvider
    {
        /// <summary>
        /// Gets the log stream name.
        /// </summary>
        /// <returns></returns>
        string GetLogStreamName();
    }
}
