namespace Cirreum.Runtime;

using Cirreum.Health;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Provides factory methods for creating domain application builders.
/// </summary>
public sealed class DomainApplication
	: IApplicationBuilder, IEndpointRouteBuilder, IAsyncDisposable {


	private const string LandingPageConfigurationName = "Cirreum:LandingPage";
	private const string LandingPageEnvVariable = "Cirreum_LANDING_PAGE";

	/// <summary>
	/// Maps a redirect for requests to the root ("/") path, to the specified relative path
	/// from an env variable (Cirreum_LANDING_PAGE) or appsetting (Cirreum:LandingPage).
	/// </summary>
	/// <remarks>
	/// <para>
	/// The value should be a relative path to a page (e.g., /scalar/v1 or /healthchecks-ui etc.)
	/// </para>
	/// <para>
	/// If no value is found, is an empty string, or is root ("/"), then no redirect is configured.
	/// </para>
	/// </remarks>
	public void UseLandingPage() {

		var customLandingPageUri = SystemEnvironment.Instance.GetEnvironmentVariable(LandingPageEnvVariable);
		if (customLandingPageUri.HasValue() is false) {
			customLandingPageUri = this.Configuration.GetSection(LandingPageConfigurationName).Value ?? "";
		}
		if (customLandingPageUri.HasValue()) {
			this.UseLandingPage(customLandingPageUri);
		}
	}

	/// <summary>
	/// Maps a redirect for requests to the root ("/"), to the specified <paramref name="customLandingPage"/>.
	/// </summary>
	/// <param name="customLandingPage">The relative path to an app page (e.g., /scalar/v1 or /healthchecks-ui etc.)</param>
	/// <remarks>
	/// <para>
	/// If the value is an empty string or root ("/"), then no redirect is configured.
	/// </para>
	/// </remarks>
	public void UseLandingPage(string customLandingPage) {

		if (string.IsNullOrWhiteSpace(customLandingPage)) {
			return;
		}
		if (customLandingPage.StartsWith('/') is false) {
			return;
		}
		if (customLandingPage == "/") {
			return;
		}
		this.MapGet("/", () => Results.Redirect(customLandingPage))
			.ExcludeFromDescription();

	}

	/// <summary>
	/// The application's configured services.
	/// </summary>
	public IServiceProvider Services => this._innerApplication.Services;

	/// <summary>
	/// The application's configured <see cref="IConfiguration"/>.
	/// </summary>
	public IConfiguration Configuration => this._innerApplication.Configuration;

	/// <summary>
	/// The application's configured <see cref="IWebHostEnvironment"/>.
	/// </summary>
	public IWebHostEnvironment Environment => this._innerApplication.Environment;

	/// <summary>
	/// Allows consumers to be notified of application lifetime events.
	/// </summary>
	public IHostApplicationLifetime Lifetime => this._innerApplication.Lifetime;

	/// <summary>
	/// The default logger for the application.
	/// </summary>
	public ILogger Logger => this._innerApplication.Logger;


	/// <summary>
	/// Use the default middleware pipeline optimized for stateless API applications.
	/// </summary>
	/// <remarks>
	/// Configures the application pipeline with the following middleware in order:
	/// - Exception handling
	/// - Forwarded headers (for proxy/load balancer scenarios)
	/// - Static files
	/// - Routing
	/// - Request timeouts
	/// - CORS (Cross-Origin Resource Sharing)
	/// - Authentication
	/// - Authorization
	/// - Output Caching (modern replacement for response caching)
	/// 
	/// <para>
	/// <strong>Not Included (By Design):</strong>
	/// </para>
	/// <list type="bullet">
	///   <item>Response Compression - Better handled at reverse proxy/CDN level</item>
	///   <item>Response Caching - Superseded by Output Caching for APIs</item>
	///   <item>Rate Limiting - Configure manually based on specific requirements</item>
	///   <item>Sessions - This framework targets stateless APIs</item>
	/// </list>
	/// </remarks>
	public void UseDefaultMiddleware() {

		// Natural Order
		// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-9.0#middleware-order
		/*
			app.UseStaticFiles();
			app.UseCookiePolicy();

			app.UseRouting();
			app.UseRateLimiter();
			app.UseRequestLocalization();
			app.UseCors();

			app.UseAuthentication();
			app.UseAuthorization();
			app.UseSession();
			app.UseResponseCompression();
			app.UseResponseCaching();
		 
		 */

		this
			.UseExceptionHandler()
			.UseForwardedHeaders()
			.UseStaticFiles()
			.UseRouting()
			.UseRequestTimeouts()
			.UseConfiguredCors() // Apply CORS policies
			.UseAuthentication() // Authenticate the user
			.UseAuthorization() // Authorize the user
			.UseOutputCache();
	}

	/// <summary>
	/// Configures the application's health check endpoints, using configurable base URI paths.
	/// </summary>
	/// <param name="responseWriter">The optional custom response writer.</param>
	/// <remarks>
	/// <para>
	/// Health checks are disabled by default. Enable them by setting 
	/// <c>Cirreum:HealthChecks:Enabled</c> to <c>true</c> in your configuration.
	/// </para>
	/// <para>
	/// Maps the following health check endpoints under a configurable base URI (defaults to "/health" if not specified):
	/// </para>
	/// <list type="bullet">
	///   <item>
	///     <term><c>/{baseUri}/startup</c></term>
	///     <description>
	///     Runs the <see cref="IStartedStatus"/> health check or any health check tagged with <c>"startup"</c> 
	///     to determine if the application has successfully started.
	///     </description>
	///   </item>
	///   <item>
	///     <term><c>/{baseUri}/liveness</c></term>
	///     <description>
	///     Returns a fixed successful response without evaluating any health checks, ensuring the application is running.
	///     </description>
	///   </item>
	///   <item>
	///     <term><c>/{baseUri}/readiness</c></term>
	///     <description>
	///     Runs all registered health checks tagged with <c>"ready"</c> to determine if the application is ready to handle requests.
	///     </description>
	///   </item>
	///   <item>
	///     <term><c>/{baseUri}/internal</c></term>
	///     <description>
	///     Runs all registered health checks, providing a comprehensive health status of the application.
	///     </description>
	///   </item>
	/// </list>
	/// <para>
	/// The base URI path can be configured through application settings using the key <see cref="HealthStrings.HealthCheckBaseUriKey"/> (<c>Cirreum:HealthChecks:BaseUri</c>).
	/// If not specified, it defaults to <see cref="HealthStrings.HealthDefaultBaseUriPath"/> (<c>"/health"</c>).
	/// </para>
	/// </remarks>
	public void MapDefaultHealthChecks(Func<HttpContext, HealthReport, Task>? responseWriter = null) {

		// Check if health checks are enabled
		var enabled = this.Configuration.GetValue<bool>(HealthStrings.HealthChecksEnabledKey, false);
		if (enabled is false) {
			if (this.Environment.IsProduction() && this.Logger.IsEnabled(LogLevel.Warning)) {
				this.Logger.LogWarning(
					"Health checks are disabled. Set '{ConfigKey}' to true for production deployments.",
					HealthStrings.HealthChecksEnabledKey);
			}
			return;
		}

		// Set up the base URI and Endpoint Group
		var healthBaseUri = this.Configuration.GetValue<string>(HealthStrings.HealthCheckBaseUriKey)
			?? HealthStrings.HealthDefaultBaseUriPath;
		var healthChecks = this._innerApplication.MapGroup(healthBaseUri);

		// Use provided responseWriter or default
		responseWriter ??= new HealthCheckOptions().ResponseWriter;

		// Factory method for creating HealthCheckOptions with consistent configuration
		HealthCheckOptions CreateOptions(Func<HealthCheckRegistration, bool> predicate) => new() {
			Predicate = predicate,
			ResponseWriter = responseWriter
		};

		/*

		 Health Checks...

			Startup:
				Checks if your application has successfully started. This check is
				separate from the liveness probe and executes during the initial
				startup phase of your application.

			Liveness:
				Checks if your application is still running and responsive.

			Readiness:
				Checks to see if a replica is ready to handle incoming requests.

			Internal:
				Checks all registered health checks regardless of the predicate filter.

		*/

		// Startup
		healthChecks
			.MapHealthChecks(HealthStrings.HealthStartupUriPath,
				CreateOptions(check => check.Tags.Contains(HealthStrings.HealthStartupTag)))
			.DisableHttpMetrics();

		// Liveness
		healthChecks
			.MapHealthChecks(HealthStrings.HealthLivenessUriPath,
				CreateOptions(_ => false))
			.DisableHttpMetrics();

		// Readiness
		healthChecks
			.MapHealthChecks(HealthStrings.HealthReadinessUriPath,
				CreateOptions(check => check.Tags.Contains(HealthStrings.HealthReadinessTag)))
			.DisableHttpMetrics();

		// Internal
		healthChecks
			.MapHealthChecks(HealthStrings.HealthInternalUriPath,
				CreateOptions(_ => true))
			.DisableHttpMetrics();

	}

	/// <inheritdoc/>
	IServiceProvider IApplicationBuilder.ApplicationServices {
		get => ((IApplicationBuilder)this._innerApplication).ApplicationServices;
		set => ((IApplicationBuilder)this._innerApplication).ApplicationServices = value;
	}

	/// <inheritdoc/>
	IFeatureCollection IApplicationBuilder.ServerFeatures => ((IApplicationBuilder)this._innerApplication).ServerFeatures;

	/// <inheritdoc/>
	IDictionary<string, object?> IApplicationBuilder.Properties => ((IApplicationBuilder)this._innerApplication).Properties;

	/// <inheritdoc/>
	IServiceProvider IEndpointRouteBuilder.ServiceProvider => ((IEndpointRouteBuilder)this._innerApplication).ServiceProvider;

	/// <inheritdoc/>
	ICollection<EndpointDataSource> IEndpointRouteBuilder.DataSources => ((IEndpointRouteBuilder)this._innerApplication).DataSources;



	/// <summary>
	/// Creates and configures a new <see cref="DomainApplicationBuilder"/> instance with default settings.
	/// </summary>
	/// <param name="args">Command line arguments passed to the application.</param>
	/// <param name="shutdownTimeoutMinutes">
	/// The maximum time, in minutes, that the host will wait for an application to shutdown.
	/// Default is 2 minutes.
	/// </param>
	/// <param name="forwardedHeaders">
	/// The forwarded headers to process. Used when the application is behind a proxy or load balancer.
	/// Default is XForwardedFor and XForwardedProto.
	/// </param>
	/// <returns>
	/// A configured <see cref="DomainApplicationBuilder"/> instance ready for further customization.
	/// </returns>
	/// <remarks>
	/// This factory method creates a builder with reasonable defaults for web applications.
	/// It configures Kestrel, forwarded headers support, request timeouts, caching, CORS,
	/// telemetry, and core infrastructure services.
	/// </remarks>
	/// <example>
	/// <code>
	/// var builder = DomainApplication.CreateBuilder(args);
	/// builder.Services.AddScoped&lt;IOrderService, OrderService&gt;();
	/// var app = builder.Build();
	/// await app.RunAsync();
	/// </code>
	/// </example>
	public static DomainApplicationBuilder CreateBuilder(
		string[] args,
		int shutdownTimeoutMinutes = 2,
		ForwardedHeaders forwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto) {
		return DomainApplicationBuilder.CreateAndConfigureBuilder(
			args,
			shutdownTimeoutMinutes,
			forwardedHeaders);
	}


	/// <inheritdoc/>
	RequestDelegate IApplicationBuilder.Build() => ((IApplicationBuilder)this._innerApplication).Build();

	/// <inheritdoc/>
	IApplicationBuilder IEndpointRouteBuilder.CreateApplicationBuilder() => ((IApplicationBuilder)this._innerApplication).New();

	/// <inheritdoc/>
	IApplicationBuilder IApplicationBuilder.New() => ((IApplicationBuilder)this._innerApplication).New();

	/// <inheritdoc/>
	public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware) => this._innerApplication.Use(middleware);

	private readonly WebApplication _innerApplication;
	internal DomainApplication(WebApplication innerApplication) {
		this._innerApplication = innerApplication;
	}

	/// <summary>
	/// Executes any registered <see cref="ISystemInitializer"/>,
	/// <see cref="IAutoInitialize"/> or <see cref="IStartupTask"/> services, and then runs
	/// the application, returning an awaitable Task that only completes when shutdown is triggered.
	/// </summary>
	/// <param name="url">The URL to listen to if the server hasn't been configured directly.</param>
	/// <returns>
	/// A <see cref="Task"/> that represents the entire runtime of the <see cref="WebApplication"/> from startup to shutdown.
	/// </returns>
	public async Task RunAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? url = null) {

		// Startup Status
		var startupStatus = this._innerApplication.Services.GetRequiredService<IStartedStatus>();

		// Initialize the application
		await this.Services.InitializeApplicationAsync();

		// Run as normal
		var runTask = this._innerApplication.RunAsync(url);

		// Ok, we've started!
		startupStatus.StartupCompleted = true;

		// wait for termination
		await runTask;

	}

	/// <summary>
	/// Disposes the application.
	/// </summary>
	public ValueTask DisposeAsync() => this._innerApplication.DisposeAsync();

}