using System.Net;
using Amazon.Runtime;
using System.Linq;

namespace Serilog.Sinks.AwsCloudWatch
{
    internal static class HttpStatusCodeExtensions
    {
        public static bool IsSuccessStatusCode(this HttpStatusCode statusCode)
        {
            if (statusCode >= HttpStatusCode.OK)
                return statusCode <= (HttpStatusCode)299;
            return false;
        }

        public static string FlattenedMetaData(this ResponseMetadata metadata)
        {
            return string.Join(",", metadata.Metadata.Select(m => $"{m.Key} => {m.Value}"));
        }
    }
}