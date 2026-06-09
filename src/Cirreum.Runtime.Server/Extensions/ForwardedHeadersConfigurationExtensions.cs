namespace Cirreum.Runtime.Extensions;

using Cirreum.Logging.Deferred;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Configures forwarded-headers processing with a platform-gated default (ADR-0023). A detected managed
/// ingress (Azure Container Apps / App Service) gets trust-one-hop <c>PlatformIngress</c>; Development and
/// undetected platforms stay on ASP.NET's loopback-only secure default.
/// </summary>
/// <remarks>
/// There is no appsettings binding and no boot fail-fast: forwarded scheme / IP / host are not load-bearing
/// for any Cirreum authentication decision, so the default exists only for generic correctness — a real
/// client IP for audit logs and a correct <c>Request.Scheme</c> behind a TLS-terminating edge. Apps with a
/// custom proxy topology override through ASP.NET's own <c>services.Configure&lt;ForwardedHeadersOptions&gt;</c>.
/// </remarks>
internal static class ForwardedHeadersConfigurationExtensions {

	/// <summary>
	/// Applies the platform-gated forwarded-headers default.
	/// </summary>
	/// <remarks>
	/// On a detected managed platform (ACA via <c>CONTAINER_APP_NAME</c>, App Service via
	/// <c>WEBSITE_SITE_NAME</c>) it trusts exactly one ingress hop — <paramref name="forwardedHeaders"/>,
	/// <c>ForwardLimit = 1</c>, cleared <c>KnownProxies</c> / <c>KnownIPNetworks</c>. That is safe because the
	/// container is reachable only through the managed ingress, and <c>ForwardLimit = 1</c> consumes the
	/// ingress-stamped rightmost <c>X-Forwarded-*</c> value while ignoring any client-forged ones. On
	/// Development or an undetected platform it leaves ASP.NET's loopback-only default in place — trust-all is
	/// never applied off a detected managed platform, which would be a spoofing hole on a directly-exposed host.
	/// </remarks>
	/// <param name="services">The service collection.</param>
	/// <param name="environment">The host environment (Development stays loopback-only).</param>
	/// <param name="forwardedHeaders">The forwarded-header flags the middleware processes (the runtime default
	/// is <c>XForwardedFor | XForwardedProto</c> — host is deliberately not forwarded).</param>
	internal static void ConfigurePlatformIngress(
		this IServiceCollection services,
		IHostEnvironment environment,
		ForwardedHeaders forwardedHeaders) {

		var managedPlatform = DetectManagedPlatform();
		var applyPlatformIngress = managedPlatform is not null && !environment.IsDevelopment();

		services.Configure<ForwardedHeadersOptions>(options => {
			options.ForwardedHeaders = forwardedHeaders;

			if (applyPlatformIngress) {
				// PlatformIngress: trust exactly the one managed-ingress hop.
				options.ForwardLimit = 1;
				options.KnownProxies.Clear();
				options.KnownIPNetworks.Clear();
			}

			// Development / undetected platform: leave KnownProxies / KnownIPNetworks at ASP.NET's loopback-only
			// default, so forwarded values are honored only from loopback (dropped behind a non-loopback proxy —
			// harmless, since no auth impl consumes scheme / IP / host).
		});

		if (applyPlatformIngress) {
			Logger.CreateDeferredLogger().LogInformation(
				"Forwarded headers: a managed platform (Azure Container Apps / App Service) was detected; applying " +
				"PlatformIngress — trusting one ingress hop, so audit logs and Request.Scheme reflect the " +
				"ingress-stamped X-Forwarded-* values (ADR-0023).");
		} else if (!environment.IsDevelopment()) {
			// No boot fail-fast (ADR-0023): an undetected-but-actually-proxied host silently drops forwarded
			// values, so leave a non-blocking breadcrumb pointing at the override rather than blocking boot.
			Logger.CreateDeferredLogger().LogInformation(
				"Forwarded headers: no managed platform detected (not Azure Container Apps or App Service), so " +
				"X-Forwarded-* are honored from loopback only. If this host is behind a proxy or load balancer and " +
				"needs the real client IP / scheme (audit, TLS-terminated scheme), configure " +
				"services.Configure<ForwardedHeadersOptions>(...) with your trusted KnownIPNetworks (ADR-0023).");
		}
	}

	private static string? DetectManagedPlatform() {
		if (Environment.GetEnvironmentVariable("CONTAINER_APP_NAME") is not null) {
			return "Azure Container Apps";
		}

		if (Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") is not null) {
			return "Azure App Service";
		}

		return null;
	}

}
