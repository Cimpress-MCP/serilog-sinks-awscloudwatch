using Amazon.CloudWatchLogs.Model;
using Serilog.Events;
using Serilog.Formatting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Serilog.Sinks.AwsCloudWatch.EventTransformer
{
    /// <summary>
    /// Interface handling transformation of Serilog event into CloudWatch one.
    /// </summary>
    public interface IEventTransformer
    {
        /// <summary>
        /// Transforms Serilog event object into CloudWatch event object.
        /// </summary>
        /// <param name="textFormatter">Serilog TextFormatter to be used</param>
        /// <param name="event">Serilog event to be transformed</param>
        /// <returns>CloudWatch event object</returns>
        InputLogEvent TransformEvent(ITextFormatter textFormatter, LogEvent @event);
    }
}
