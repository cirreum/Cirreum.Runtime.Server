namespace Cirreum.Runtime;

using Cirreum.Health;
using Microsoft.AspNetCore.Antiforgery;
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
/// The built Cirreum server application — a wrapper over <see cref="WebApplication"/> that
/// adds the framework's default middleware pipeline, framework-owned endpoints, health-check
/// mapping, an optional landing-page redirect, and an initialization-aware
/// <see cref="RunAsync"/>. Also hosts the static <see cref="CreateBuilder"/> entry point that
/// begins composition.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="IApplicationBuilder"/> and <see cref="IEndpointRouteBuilder"/>, so
/// the standard <c>Use*</c> middleware and <c>Map*</c> endpoint extensions — including
/// <c>MapApiEndpoints</c>, which applies the framework's Result-to-HTTP filter to a route
/// group — compose directly against it.
/// </para>
/// <para>
/// The typical flow: <see cref="CreateBuilder"/> → configure services →
/// <see cref="DomainApplicationBuilder.Build()"/> → <see cref="UseDefaultMiddleware"/> →
/// <c>MapApiEndpoints</c> / <see cref="MapDefaultHealthChecks"/> /
/// <see cref="UseLandingPage()"/> → <see cref="RunAsync"/>. <see cref="RunAsync"/> executes
/// the registered initializers (<see cref="ISystemInitializer"/>, <see cref="IAutoInitialize"/>,
/// <see cref="IStartupTask"/>) before starting the host and marks startup complete for the
/// startup health probe once running.
/// </para>
/// <para>
/// Dispose with <c>await using</c> — the type is <see cref="IAsyncDisposable"/> only, by
/// design. Synchronous disposal of a host tears down the root service provider
/// synchronously, which throws when any registered singleton implements only
/// <see cref="IAsyncDisposable"/> — true of most modern SDK clients and of the framework's
/// own provider clients — so the failure would appear or disappear with the app's
/// composition. The sync path is omitted rather than documented against; the only
/// legitimate call site (an async <c>Program</c> awaiting <see cref="RunAsync"/>) pays
/// nothing for the constraint.
/// </para>
/// </remarks>
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

		var customLandingPageUri = System.Environment.GetEnvironmentVariable(LandingPageEnvVariable);
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
	/// Configures the default middleware pipeline for the domain runtime.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The configured pipeline varies slightly by <see cref="DomainRuntimeType"/>.
	/// <see cref="DomainRuntimeType.WebApi"/> targets stateless, bearer- and
	/// machine-authenticated APIs and does not include antiforgery middleware.
	/// <see cref="DomainRuntimeType.WebApp"/> additionally includes antiforgery protection
	/// for cookie-authenticated browser applications.
	/// </para>
	/// <para>
	/// Web API endpoints that bind form data and do not use ambient credentials should
	/// explicitly opt out of antiforgery validation with <c>DisableAntiforgery</c>.
	/// </para>
	/// <para>
	/// <strong>Configures the following middleware, in order:</strong>
	/// </para>
	/// <list type="bullet">
	///   <item>Exception handling</item>
	///   <item>Forwarded headers for proxy and load-balancer scenarios</item>
	///   <item>Static files</item>
	///   <item>Routing</item>
	///   <item>Request timeouts</item>
	///   <item>CORS (Cross-Origin Resource Sharing)</item>
	///   <item>Authentication</item>
	///   <item>Authorization</item>
	///   <item>Antiforgery for WebApp runtimes</item>
	///   <item>Invocation context (HTTP → <c>IInvocationContext</c> bridge)</item>
	///   <item>Output caching</item>
	/// </list>
	/// <para>
	/// <strong>Not included by design:</strong>
	/// </para>
	/// <list type="bullet">
	///   <item>Response compression — typically better handled by a reverse proxy or CDN</item>
	///   <item>Response caching — superseded by output caching</item>
	///   <item>Rate limiting — configure explicitly based on application requirements</item>
	///   <item>Sessions — Cirreum applications are expected to remain stateless</item>
	/// </list>
	/// </remarks>
	public void UseDefaultMiddleware() {

		// Natural Order
		// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/middleware
		this
			.UseExceptionHandler()
			.UseForwardedHeaders()
			.UseStaticFiles()
			.UseRouting()
			.UseRequestTimeouts()
			.UseConfiguredCors()  // Apply CORS policies
			.UseAuthentication() // Authenticate the user
			.UseAuthorization(); // Authorize the user

		// Antiforgery middleware must run after authentication and authorization
		// to prevent reading form data when the user is unauthenticated.
		var domainEnv = this.Services.GetRequiredService<IDomainEnvironment>();
		if (domainEnv.RuntimeType == DomainRuntimeType.WebApp) {
			this.UseAntiforgery();
		}

		// Add support for Cirreum invocation features, providing access to
		// the current HTTP context and other request-specific information.
		this.UseInvocationContext();

		// Output caching should be applied after authentication and authorization
		// to ensure cached responses are served only to authorized users.
		this.UseOutputCache();

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
	/// <para>
	/// Optionally, the health check endpoints can be restricted to a specific host by setting <see cref="HealthStrings.HealthCheckHostKey"/> (<c>Cirreum:HealthChecks:Host</c>).
	/// This uses ASP.NET Core's host matching format, which supports wildcards and port specifications (e.g., <c>*:8081</c>, <c>localhost:5001</c>, <c>management.example.com:8090</c>).
	/// This is useful for exposing health checks only on an internal port or management interface, preventing external access to detailed health information.
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
		var healthHost = this.Configuration.GetValue<string>(HealthStrings.HealthCheckHostKey);
		var healthChecks = this._innerApplication.MapGroup(healthBaseUri);
		if (string.IsNullOrWhiteSpace(healthHost) is false) {
			healthChecks.RequireHost(healthHost);
		}

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

	/// <summary>
	/// Maps an authenticated endpoint that issues an antiforgery request token.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This endpoint is available only for <see cref="DomainRuntimeType.WebApp"/> runtimes
	/// and throws if mapped for any other runtime type.
	/// </para>
	/// <para>
	/// The endpoint stores the antiforgery cookie and returns the corresponding request token.
	/// Clients should include the returned token in subsequent protected requests using
	/// <see cref="DomainAntiforgeryDefaults.HeaderName"/>.
	/// </para>
	/// </remarks>
	/// <param name="pattern">
	/// The route pattern for the token endpoint.
	/// Defaults to <see cref="DomainAntiforgeryDefaults.TokenEndpoint"/>.
	/// </param>
	/// <returns>
	/// The endpoint convention builder for the mapped antiforgery token endpoint.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// The domain runtime is not configured as <see cref="DomainRuntimeType.WebApp"/>.
	/// </exception>
	public IEndpointConventionBuilder MapDefaultAntiforgeryToken(
		string pattern = DomainAntiforgeryDefaults.TokenEndpoint) {

		var domainEnv = this.Services.GetRequiredService<IDomainEnvironment>();
		if (domainEnv.RuntimeType != DomainRuntimeType.WebApp) {
			throw new InvalidOperationException(
				"Antiforgery token endpoints are only supported for WebApp domain runtimes.");
		}

		return this
			.MapGet(pattern, (
				IAntiforgery antiforgery,
				HttpContext context) => {
					var tokens = antiforgery.GetAndStoreTokens(context);
					return Results.Ok(new {
						token = tokens.RequestToken
					});
				})
			.RequireAuthorization()
			.ExcludeFromDescription();
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