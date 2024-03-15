using Serilog.Events;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.AwsCloudWatch.Tests
{
    public static class CloudWatchLogsSinkExtensions
    {
        /// <summary>
        /// Extension method to call protected EmitBatchAsync method.
        /// </summary>
        /// <param name="sink">The sink.</param>
        /// <param name="events">The events to be published.</param>
        /// <returns>The task to wait on.</returns>
        public static Task EmitBatchAsync(this CloudWatchLogSink sink, IEnumerable<LogEvent> events)
        {
            return ((IBatchedLogEventSink)sink).EmitBatchAsync(events);
        }
    }
}
