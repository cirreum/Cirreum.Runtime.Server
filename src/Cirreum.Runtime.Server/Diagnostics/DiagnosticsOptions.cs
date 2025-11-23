namespace Cirreum.Runtime.Diagnostics;

/// <summary>
/// Configuration options for OpenTelemetry diagnostics including metrics, tracing, and logging.
/// </summary>
/// <remarks>
/// <para>
/// This class configures telemetry collection and export for the Cirreum framework, supporting
/// multiple exporters including OTLP and Azure Monitor (Application Insights).
/// </para>
/// <para>
/// Configuration can be provided via appsettings.json under the "Cirreum:Diagnostics" section.
/// </para>
/// </remarks>
/// <example>
/// Example configuration in appsettings.json:
/// <code>
/// {
///   "Cirreum": {
///     "Diagnostics": {
///       "Disabled": false,
///       "ServiceNamespace": "app.backend",
///       "TraceSamplingRatio": 0.2,
///       "OtlpExporter": {
///         "Disabled": false,
///         "OtlpEndpoint": "http://localhost:4317",
///         "OtlpProtocol": "grpc"
///       },
///       "AzureMonitor": {
///         "Disabled": false,
///         "EnableDefaultCredentials": true
///       }
///     }
///   }
/// }
/// </code>
/// </example>
public class DiagnosticsOptions {
	public static readonly string DiagnosticsConfigurationName = "Cirreum:Diagnostics";
	/// <summary>
	/// Enable or disable telemetry.
	/// </summary>
	public bool Disabled { get; set; }
	/// <summary>
	/// The service namespace for grouping related services in observability tools.
	/// Default is "app.backend".
	/// </summary>
	public string ServiceNamespace { get; set; } = "app.backend";
	/// <summary>
	/// Trace sampling ratio (0.0 to 1.0). 
	/// Controls the percentage of traces collected.
	/// Default is 1.0 (100%) in development, 0.2 (20%) in production.
	/// </summary>
	/// <remarks>
	/// Set to 1.0 to capture all traces, or lower values like 0.1 (10%) for high-traffic production.
	/// </remarks>
	public double? TraceSamplingRatio { get; set; }
	/// <summary>
	/// Configuration options for OtlpExporter.
	/// </summary>
	public OtlpExporterOptions OtlpExporter { get; set; } = new();
	/// <summary>
	/// Configuration options for AzureMonitor.
	/// </summary>
	public AzureMonitorOptions AzureMonitor { get; set; } = new();
}