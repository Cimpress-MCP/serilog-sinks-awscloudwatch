using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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
            var emitMethod = sink.GetType().GetMethod("EmitBatchAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            return emitMethod.Invoke(sink, new object[] { events }) as Task;
        }
    }
}
