using System;
using System.Net;
using System.Text;

namespace Serilog.Sinks.AwsCloudWatch.LogStreamNameProvider
{
	/// <summary>
	/// Provides a configurable log stream name provider allowing you to use just a
	/// stream prefix or to optionally append the hostname of the client and a GUID
	/// </summary>
	public class ConfigurableLogStreamNameProvider : ILogStreamNameProvider
	{
		private readonly string logStreamPrefix;
		private readonly Guid instanceGuid = Guid.NewGuid();
		private readonly bool appendHostName;
		private readonly bool appendUniqueInstanceGuid;

		private string streamName = null;

		/// <summary>
		/// Create a Log Stream Name Provider which by default creates a log stream prefix like:
		/// <code>
		/// {logStreamPrefix}/{hostName}/{randomGuid}
		/// </code>
		/// The guid is random per instance meaning that each instance of the <see cref="ConfigurableLogStreamNameProvider"/>
		/// will have a different unique guid. You can suppress the unique guid or the hostname from
		/// being used by setting the <paramref name="appendUniqueInstanceGuid"/> and <paramref name="appendHostName"/>
		/// parameters respectively
		/// </summary>
		/// <param name="logStreamPrefix">Base log stream prefix i.e. "MyApplication/MyComponent" (Required)</param>
		/// <param name="appendHostName">Should the hostname be appended to the log stream prefix?</param>
		/// <param name="appendUniqueInstanceGuid">Should a unique GUID be appended to the log stream prefix?</param>
		public ConfigurableLogStreamNameProvider(
			string logStreamPrefix, 
			bool appendHostName = true,
			bool appendUniqueInstanceGuid = true)
		{
			if (String.IsNullOrWhiteSpace(logStreamPrefix))
			{
				throw new ArgumentException("You must provide a log stream prefix", nameof(logStreamPrefix));
			}

			this.logStreamPrefix = logStreamPrefix;
			this.appendHostName = appendHostName;
			this.appendUniqueInstanceGuid = appendUniqueInstanceGuid;
		}

		/// <inheritdoc cref="ILogStreamNameProvider"/>
		public string GetLogStreamName()
		{
			if (String.IsNullOrEmpty(streamName))
			{
				var sb = new StringBuilder(logStreamPrefix);
				if (appendHostName)
				{
					var instanceName = Dns.GetHostName();
					sb.Append($"/{instanceName}");
				}

				if (appendUniqueInstanceGuid)
				{
					sb.Append($"/{instanceGuid:N}");
				}

				streamName = sb.ToString();
			}

			return streamName;
		}
	}
}
