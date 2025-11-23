namespace Cirreum.Runtime.Diagnostics;

public class OtlpExporterOptions {

	/// <summary>
	/// Enabled/Disabled
	/// </summary>
	public bool Disabled { get; set; }

	/// <summary>
	/// Target to which the exporter is going to send traces or metrics.
	/// </summary>
	/// <remarks>
	/// The connection string to use, when the Environment Variable 'OTEL_EXPORTER_OTLP_ENDPOINT'
	/// doesn't exist or is empty.
	/// <para>
	/// If neither setting contains a value, the Otlp exporting is not enabled.
	/// </para>
	/// <para>
	/// The endpoint must be a valid Uri with scheme (http or https) and host, and MAY contain a port and path.
	/// The default is "localhost:4317" for Grpc and "localhost:4318" for HttpProtobuf.
	/// </para>
	/// <para>
	/// NOTE: When using HttpProtobuf with <see cref="OtlpProtocol"/>, the full URL MUST be provided, including
	/// the signal-specific  path v1/{signal}. For example, for traces, the full URL will look 
	/// like http://your-custom-endpoint/v1/traces.
	/// </para>
	/// </remarks>
	public string OtlpEndpoint { get; set; } = "";

	/// <summary>
	/// OTLP transport protocol. Supported values: Grpc and HttpProtobuf. The default is Grpc
	/// </summary>
	public string OtlpProtocol { get; set; } = "grpc";

}