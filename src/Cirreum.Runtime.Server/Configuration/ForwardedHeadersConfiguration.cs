namespace Cirreum.Runtime.Configuration;

using Microsoft.AspNetCore.HttpOverrides;

/// <summary>
/// Binds the <c>Cirreum:ForwardedHeaders</c> configuration section into ASP.NET's
/// <see cref="ForwardedHeadersOptions"/>, giving operators a safe, declarative way to declare
/// their trusted-proxy topology (ADR-0023).
/// </summary>
/// <remarks>
/// <para>
/// The framework enables forwarded-headers processing for every server app, but the secure
/// default trusts only loopback. Behind a real proxy (ACA / App Service / k8s / APIM / load
/// balancer) the proxy connects from a non-loopback address, so forwarded headers are dropped
/// unless the proxy topology is declared here.
/// </para>
/// <para>
/// In a non-Development environment, the build fails fast unless exactly one posture is declared:
/// configure <see cref="KnownProxies"/> / <see cref="KnownNetworks"/>, opt into
/// <see cref="TrustAllProxies"/>, or set <see cref="AcknowledgeLoopbackOnly"/>. This prevents a
/// Development configuration from silently reaching Production with forwarded values mis-trusted.
/// </para>
/// </remarks>
public sealed class ForwardedHeadersConfiguration {

	/// <summary>
	/// The configuration section name: <c>Cirreum:ForwardedHeaders</c>.
	/// </summary>
	public const string SectionName = "Cirreum:ForwardedHeaders";

	/// <summary>
	/// Known trusted proxy IP addresses. A forwarded header is honored only when the immediate
	/// peer is in this set (or <see cref="KnownNetworks"/>).
	/// </summary>
	public string[] KnownProxies { get; set; } = [];

	/// <summary>
	/// Known trusted proxy networks in CIDR notation (e.g. <c>10.0.0.0/8</c>).
	/// </summary>
	public string[] KnownNetworks { get; set; } = [];

	/// <summary>
	/// Maximum number of forwarded-header entries to process (proxy hops). Leave unset to use the
	/// ASP.NET default; set higher for multi-hop topologies (edge → APIM → app).
	/// </summary>
	public int? ForwardLimit { get; set; }

	/// <summary>
	/// Overrides which forwarded headers are processed. When unset, the runtime default
	/// (<see cref="ForwardedHeaders.XForwardedFor"/> | <see cref="ForwardedHeaders.XForwardedProto"/>) is used.
	/// </summary>
	public ForwardedHeaders? Headers { get; set; }

	/// <summary>
	/// Opts <see cref="ForwardedHeaders.XForwardedHost"/> into the processed set. Off by default;
	/// enable only when the app needs the original client-facing host (e.g. ADR-0021 <c>@authority</c>
	/// signing, absolute-link generation). Meaningful only with trusted proxies configured.
	/// </summary>
	public bool ForwardHost { get; set; }

	/// <summary>
	/// Escape hatch for environments that cannot enumerate proxy addresses (some PaaS): trust
	/// forwarded headers from ANY peer. This is spoofable — it is a deliberate, logged choice,
	/// never a silent default. Replaces the <c>KnownProxies.Clear()</c> footgun.
	/// </summary>
	public bool TrustAllProxies { get; set; }

	/// <summary>
	/// Explicitly acknowledges a genuine no-proxy (loopback-only) deployment, allowing a
	/// non-Development host to boot without declaring proxies. Behind a proxy the client
	/// scheme / IP / host will be dropped.
	/// </summary>
	public bool AcknowledgeLoopbackOnly { get; set; }

}
