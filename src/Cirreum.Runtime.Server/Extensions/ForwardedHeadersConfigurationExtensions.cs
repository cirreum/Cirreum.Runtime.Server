namespace Cirreum.Runtime.Extensions;

using Cirreum.Logging.Deferred;
using Cirreum.Runtime.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;

/// <summary>
/// Binds the <c>Cirreum:ForwardedHeaders</c> section into <see cref="ForwardedHeadersOptions"/>
/// and enforces the trusted-proxy boot posture (ADR-0023).
/// </summary>
internal static class ForwardedHeadersConfigurationExtensions {

	/// <summary>
	/// Configures forwarded-headers processing from the <c>Cirreum:ForwardedHeaders</c> section and
	/// validates the trusted-proxy posture. The validation emits a deferred <c>Error</c> (which the
	/// always-on <c>ValidateDeferredLogs()</c> turns into a fail-fast at <c>Build()</c>) when a
	/// non-Development host enables forwarding without declaring a posture.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="configuration">The application configuration.</param>
	/// <param name="environment">The host environment (Development is exempt from fail-fast).</param>
	/// <param name="defaultHeaders">The runtime default forwarded-headers flags, used when the config does not override <see cref="ForwardedHeadersConfiguration.Headers"/>.</param>
	internal static void ConfigureForwardedHeaders(
		this IServiceCollection services,
		IConfiguration configuration,
		IHostEnvironment environment,
		ForwardedHeaders defaultHeaders) {

		var config = configuration
			.GetSection(ForwardedHeadersConfiguration.SectionName)
			.Get<ForwardedHeadersConfiguration>() ?? new();

		var headers = config.Headers ?? defaultHeaders;
		if (config.ForwardHost) {
			headers |= ForwardedHeaders.XForwardedHost;
		}

		var hasExplicitProxies = config.KnownProxies.Length > 0 || config.KnownNetworks.Length > 0;

		services.Configure<ForwardedHeadersOptions>(options => {
			options.ForwardedHeaders = headers;

			if (config.ForwardLimit.HasValue) {
				options.ForwardLimit = config.ForwardLimit;
			}

			if (config.TrustAllProxies) {
				// Explicit, logged escape hatch: honor forwarded headers from ANY peer.
				options.KnownProxies.Clear();
				options.KnownIPNetworks.Clear();
				return;
			}

			foreach (var proxy in config.KnownProxies) {
				if (IPAddress.TryParse(proxy, out var ip)) {
					options.KnownProxies.Add(ip);
				}
			}

			foreach (var network in config.KnownNetworks) {
				if (System.Net.IPNetwork.TryParse(network, out var net)) {
					options.KnownIPNetworks.Add(net);
				}
			}
		});

		ValidatePosture(environment, headers, hasExplicitProxies, config);
	}

	private static void ValidatePosture(
		IHostEnvironment environment,
		ForwardedHeaders headers,
		bool hasExplicitProxies,
		ForwardedHeadersConfiguration config) {

		// Development is exempt — loopback-only is correct locally and needs no declaration.
		if (environment.IsDevelopment()) {
			return;
		}

		// Forwarding effectively disabled — nothing to validate.
		if (headers == ForwardedHeaders.None) {
			return;
		}

		var logger = Logger.CreateDeferredLogger();

		// Deliberate trust-all posture: surfaced (Information), never blocking.
		if (config.TrustAllProxies) {
			logger.LogInformation(
				"Forwarded-headers trust-all escape hatch is enabled (Cirreum:ForwardedHeaders:TrustAllProxies=true): " +
				"X-Forwarded-* headers are honored from ANY peer, which is spoofable. Deliberate posture for " +
				"environments that cannot enumerate proxy addresses (ADR-0023).");
			return;
		}

		// Proper trusted-proxy posture declared — nothing to surface.
		if (hasExplicitProxies) {
			return;
		}

		// Deliberate loopback-only posture: surfaced (Information), never blocking.
		if (config.AcknowledgeLoopbackOnly) {
			logger.LogInformation(
				"Forwarded-headers posture is loopback-only (Cirreum:ForwardedHeaders:AcknowledgeLoopbackOnly=true): " +
				"X-Forwarded-* headers are honored only from loopback. Correct for a no-proxy deployment; behind a " +
				"proxy the client scheme / IP / host will be dropped (ADR-0023).");
			return;
		}

		// Non-Development + forwarding enabled + no posture declared → fail fast (deferred Error).
		logger.LogError(
			"Forwarded-headers processing is enabled in a non-Development environment but no trusted-proxy posture " +
			"is declared. Configure Cirreum:ForwardedHeaders:KnownProxies / KnownNetworks for your proxy topology, " +
			"or set TrustAllProxies=true (un-enumerable PaaS edge; spoofable), or set AcknowledgeLoopbackOnly=true " +
			"(genuine no-proxy deployment). Boot is blocked to prevent a Development configuration silently reaching " +
			"Production with forwarded values mis-trusted (ADR-0023).");
	}

}
