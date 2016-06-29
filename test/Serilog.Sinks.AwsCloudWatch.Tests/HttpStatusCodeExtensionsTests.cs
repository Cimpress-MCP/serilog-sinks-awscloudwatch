using System.Linq;
using System.Net;
using Amazon.Runtime;
using Xunit;

namespace Serilog.Sinks.AwsCloudWatch.Tests
{
    public class HttpStatusCodeExtensionsTests
    {
        [Theory]
        [InlineData(HttpStatusCode.OK, true)]
        [InlineData(HttpStatusCode.Created, true)]
        [InlineData(HttpStatusCode.BadRequest, false)]
        [InlineData(HttpStatusCode.InternalServerError, false)]
        public void CheckForSuccessCodeReturnsCorrectBooleanValue(HttpStatusCode code, bool isSuccess)
        {
            Assert.Equal(code.IsSuccessStatusCode(), isSuccess);
        }

        [Fact]
        public void FlattenMetadataUsesCommaSeparation()
        {
            ResponseMetadata metadata = new ResponseMetadata();
            foreach (var entry in Enumerable.Range(1, 10).ToDictionary(x => x.ToString(), y => y.ToString()))
            {
                metadata.Metadata.Add(entry);
            }
            var result = metadata.FlattenedMetaData();
            foreach (var entry in Enumerable.Range(1, 10))
            {
                Assert.True(result.IndexOf(entry + " => " + entry) > -1);
            }
        }
    }
}
