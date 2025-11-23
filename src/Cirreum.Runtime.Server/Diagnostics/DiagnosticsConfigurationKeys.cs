namespace Cirreum.Runtime.Diagnostics;

/// <summary>
/// Configuration keys for telemetry and diagnostics.
/// </summary>
public static class DiagnosticsConfigurationKeys {

	// OTLP
	public const string OtlpEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";
	public const string OtelHttpProtocol = "HttpProtobuf";

	// Azure Monitor / Application Insights
	public const string AppInsightsConnectionStringEnv = "APPLICATIONINSIGHTS_CONNECTION_STRING";
	public const string AzureMonitorConnectionString = "AzureMonitor:ConnectionString";
	public const string ApplicationInsightsConnectionString = "ApplicationInsights:ConnectionString";
	public const string ApplicationInsightsConnectionStringKey = "ApplicationInsights";

}