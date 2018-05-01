namespace Serilog.Sinks.AwsCloudWatch.LogStreamNameProvider
{
	public class InstanceIdLogStreamNameProvider : ILogStreamNameProvider
	{
		private readonly string _instanceId;

		public InstanceIdLogStreamNameProvider()
		{
			_instanceId = Amazon.Util.EC2InstanceMetadata.InstanceId;
		}

		public string GetLogStreamName()
		{
			return _instanceId;
		}
	}
}
