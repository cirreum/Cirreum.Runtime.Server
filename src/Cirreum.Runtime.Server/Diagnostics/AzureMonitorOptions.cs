namespace Cirreum.Runtime.Diagnostics;

public class AzureMonitorOptions {

	/// <summary>
	/// Enabled/Disabled
	/// </summary>
	public bool Disabled { get; set; }

	/// <summary>
	/// Set the credentials to DefaultAzureCredential.
	/// </summary>
	public bool EnableDefaultCredentials { get; set; }

	/// <summary>
	/// Gets or sets value indicating whether distributed tracing activities (System.Diagnostics.Activity)
	/// are going to be created for the clients methods calls and HTTP calls.
	/// </summary>
	public bool IsDistributedTracingEnabled { get; set; }

	/// <summary>
	/// Gets or sets the fallback connection string to use for Azure Monitor logging.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The connection string is resolved in the following priority order:
	/// </para>
	/// <list type="number">
	///     <item>
	///         <description>Environment Variable: 'APPLICATIONINSIGHTS_CONNECTION_STRING'</description>
	///     </item>
	///     <item>
	///         <description>JSON Configuration: 'AzureMonitor:ConnectionString'</description>
	///     </item>
	///     <item>
	///         <description>JSON Configuration: 'ApplicationInsights:ConnectionString'</description>
	///     </item>
	///     <item>
	///         <description>JSON Configuration: 'ConnectionStrings:ApplicationInsights'</description>
	///     </item>
	///     <item>
	///         <description>This property value (fallback)</description>
	///     </item>
	/// </list>
	/// <para>
	/// Resolution stops at the first non-empty value found. If no valid connection string is found
	/// in any location, Azure Monitor will not be configured.
	/// </para>
	/// </remarks>
	/// <value>
	/// The fallback connection string to use for Azure Monitor logging. Can be null.
	/// </value>
	/// <example>
	/// Setting the fallback connection string:
	/// <code>
	/// options.ApplicationInsights = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://...";
	/// </code>
	/// </example>
	public string? ApplicationInsights { get; set; }

}