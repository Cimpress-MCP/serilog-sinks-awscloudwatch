using System;

namespace Serilog.Sinks.AwsCloudWatch
{
#if NET451
    [Serializable]
#endif
    /// <summary>
    /// Describes an exception originating from this library
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class AwsCloudWatchSinkException : Exception
    {
        public AwsCloudWatchSinkException(string message) : base(message) { }
        public AwsCloudWatchSinkException(string message, Exception inner) : base(message, inner) { }
#if NET451
        protected AwsCloudWatchSinkException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}
