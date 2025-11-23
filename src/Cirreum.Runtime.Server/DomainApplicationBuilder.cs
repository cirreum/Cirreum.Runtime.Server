namespace Cirreum.Runtime;

using Azure.Identity;
using Cirreum.Conductor.Configuration;
using Cirreum.Diagnostics;
using Cirreum.Logging.Deferred;
using Cirreum.Runtime.Diagnostics;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;

/// <summary>
/// A builder for ASP.NET Core web applications that integrates domain services,
/// authorization, validation, and CQRS features.
/// </summary>
/// <remarks>
/// <para>
/// This builder extends the standard WebApplicationBuilder with additional features
/// for domain-driven applications. It provides configuration for telemetry, health checks,
/// CORS, and other common infrastructure services.
/// </para>
/// <para>
/// Use the <see cref="DomainApplication.CreateBuilder(string[], int, Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders)"/>
/// method to create a pre-configured instance of this builder. Then, configure additional services as needed before
/// calling one of the <see cref="Build()"/> methods.
/// </para>
/// <para>
/// The builder supports specifying additional assemblies containing domain services,
/// validators, and authorization handlers through the <see cref="DomainServicesBuilder"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var builder = DomainApplication.CreateBuilder(args);
/// 
/// // Build the application
/// using var app = builder.Build&lt;ReferencedAsm1&gt;();
/// 
/// // Configure middleware
/// app.UseDefaultMiddleware();
/// 
/// // Map endpoints
/// app.MapEndpoints();
/// 
/// // Run the application
/// await app.InitializeAndRunAsync();
/// </code>
/// </example>
public sealed class DomainApplicationBuilder
	: IServerDomainApplicationBuilder, IHostApplicationBuilder {

	private Action<ConductorOptionsBuilder>? _conductorConfiguration;

	internal static DomainApplicationBuilder CreateAndConfigureBuilder(
		string[] args,
		int shutdownTimeoutMinutes = 2,
		ForwardedHeaders forwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto) {

		var webAppBuilder = new DomainApplicationBuilder(WebApplication.CreateBuilder(args));

		webAppBuilder.ConfigureBuilder(shutdownTimeoutMinutes, forwardedHeaders);

		return webAppBuilder;

	}

	private readonly WebApplicationBuilder _innerBuilder;

	/// <inheritdoc/>
	public IEnumerable<string> GetDeferredLogMessages() {
		return Logger.GetAll().Select(e => e.Message);
	}

	/// <inheritdoc/>
	IConfigurationManager IServerDomainApplicationBuilder.Configuration => this._innerBuilder.Configuration;

	/// <inheritdoc/>
	IConfigurationManager IHostApplicationBuilder.Configuration => this._innerBuilder.Configuration;

	/// <summary>
	/// A collection of configuration providers for the application to compose. This is useful for adding new configuration sources and providers.
	/// </summary>
	public ConfigurationManager Configuration => this._innerBuilder.Configuration;

	/// <inheritdoc/>
	IHostEnvironment IServerDomainApplicationBuilder.Environment => this._innerBuilder.Environment;

	/// <inheritdoc/>
	IHostEnvironment IHostApplicationBuilder.Environment => this._innerBuilder.Environment;

	/// <summary>
	/// Provides information about the web hosting environment an application is running.
	/// </summary>
	public IWebHostEnvironment Environment => this._innerBuilder.Environment;

	/// <inheritdoc/>
	public ILoggingBuilder Logging => this._innerBuilder.Logging;

	/// <inheritdoc/>
	public IServiceCollection Services => this._innerBuilder.Services;

	/// <inheritdoc/>
	IMetricsBuilder IHostApplicationBuilder.Metrics => this._innerBuilder.Metrics;

	/// <inheritdoc/>
	IDictionary<object, object> IHostApplicationBuilder.Properties => ((IHostApplicationBuilder)this._innerBuilder).Properties;


	/// <summary>
	/// Constructor
	/// </summary>
	private DomainApplicationBuilder(WebApplicationBuilder webApplicationBuilder) {
		this._innerBuilder = webApplicationBuilder;
	}

	private void ConfigureTelemetry() {

		var diagnosticSection = this.Configuration.GetSection(DiagnosticsOptions.DiagnosticsConfigurationName);
		var diagnosticOptions = diagnosticSection?.Get<DiagnosticsOptions>();
		if (diagnosticOptions?.Disabled is true) {
			return;
		}

		// Basic Logging Options
		this.Logging.AddOpenTelemetry(logging => {
			logging.IncludeFormattedMessage = true;
			logging.IncludeScopes = true;
		});

		// Open Telemetry
		var appNamespace = diagnosticOptions?.ServiceNamespace ?? "app.backend";
		var asmName = Assembly.GetEntryAssembly()?.GetName();
		var appVersion = asmName?.Version?.ToString() ?? "0.0.0";
		var appName = this.Environment.ApplicationName;
		var appId = SystemEnvironment.Instance.MachineName ?? Guid.NewGuid().ToString("n");
		var otelBuilder = this.Services.AddOpenTelemetry()
			.ConfigureResource(resource =>
				resource.AddService(
					serviceName: appName,
					serviceNamespace: appNamespace,
					serviceVersion: appVersion,
					serviceInstanceId: appId))
			.WithMetrics(metrics => {
				metrics.AddAspNetCoreInstrumentation()
					   .AddHttpClientInstrumentation()
					   .AddRuntimeInstrumentation()
					   .AddMeter(CirreumTelemetry.Meters.ConductorDispatcher)
					   .AddMeter(CirreumTelemetry.Meters.ConductorPublisher)
					   .AddMeter(CirreumTelemetry.Meters.ConductorCache)
					   .AddMeter(CirreumTelemetry.Meters.RemoteServicesClient);
			})
			.WithTracing(tracing => {
				var samplingRatio =
					diagnosticOptions?.TraceSamplingRatio
					?? (this.Environment.IsDevelopment()
						? 1.0
						: 0.2);
				tracing.SetSampler(new TraceIdRatioBasedSampler(samplingRatio));
				tracing
					.AddAspNetCoreInstrumentation()
					.AddHttpClientInstrumentation()
					.AddSource(CirreumTelemetry.ActivitySources.ConductorDispatcher)
					.AddSource(CirreumTelemetry.ActivitySources.ConductorPublisher)
					.AddSource(CirreumTelemetry.ActivitySources.RemoteServicesClient);
			});

		// OTLP Exporter
		var otlpExporter = diagnosticOptions?.OtlpExporter;
		var otlpEndpoint = this.Configuration[DiagnosticsConfigurationKeys.OtlpEndpoint] ?? otlpExporter?.OtlpEndpoint;
		if (otlpEndpoint.HasValue() && otlpExporter?.Disabled is not true) {
			if (otlpExporter?.OtlpProtocol is not null) {
				var protocol = otlpExporter.OtlpProtocol.Equals(DiagnosticsConfigurationKeys.OtelHttpProtocol, StringComparison.OrdinalIgnoreCase)
					? OtlpExportProtocol.HttpProtobuf
					: OtlpExportProtocol.Grpc;
				otelBuilder.UseOtlpExporter(protocol, new Uri(otlpEndpoint));
			} else {
				otelBuilder.UseOtlpExporter(); // Uses env var
			}
		}

		// Azure Monitor (AppInsights) Exporter
		var azureMonitor = diagnosticOptions?.AzureMonitor;
		var appInsightConnStr = this.ResolveAppInsightsConnectionString(azureMonitor);
		if (appInsightConnStr.HasValue() && azureMonitor?.Disabled is not true) {
			otelBuilder
			  .UseAzureMonitor(o => {
				  o.ConnectionString = appInsightConnStr;
				  if (azureMonitor?.EnableDefaultCredentials is true) {
					  o.Credential = new DefaultAzureCredential();
				  }
				  if (this.Environment.IsDevelopment() is false) {
					  o.SamplingRatio = 0.5F;
				  }
				  if (azureMonitor?.IsDistributedTracingEnabled is true) {
					  o.Diagnostics.IsDistributedTracingEnabled = true;
				  }
			  });
		}

	}
	private string? ResolveAppInsightsConnectionString(AzureMonitorOptions? azureMonitor) {
		return SystemEnvironment.Instance.GetEnvironmentVariable(
				DiagnosticsConfigurationKeys.AppInsightsConnectionStringEnv)
			?? this.Configuration[DiagnosticsConfigurationKeys.AzureMonitorConnectionString]
			?? this.Configuration[DiagnosticsConfigurationKeys.ApplicationInsightsConnectionString]
			?? this.Configuration.GetConnectionString(DiagnosticsConfigurationKeys.ApplicationInsightsConnectionStringKey)
			?? azureMonitor?.ApplicationInsights;
	}

	private static void ValidateDeferredLogs() {
		var issues = new[] {
			(Level: LogLevel.Warning, Logs: Logger.GetAll(LogLevel.Warning)),
			(Level: LogLevel.Error, Logs: Logger.GetAll(LogLevel.Error)),
			(Level: LogLevel.Critical, Logs: Logger.GetAll(LogLevel.Critical))
		};

		foreach (var (level, logs) in issues) {
			if (logs.Any()) {
				throw new ConfigurationException(
					$"Configuration validation failed. The following {level} items were found:\n" +
					string.Join("\n", logs.Select(l => l.Message))
				);
			}
		}

	}

	private void ConfigureBuilder(
		int shutdownTimeoutMinutes = 2,
		ForwardedHeaders forwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto) {

		// ******************************************************************************
		// Configure Global Options
		//

		// Configure Kestrel
		this._innerBuilder.WebHost
			.ConfigureKestrel(o => {
				o.AddServerHeader = false;
			});

		// Support Web-Farm/Clustering
		this.Services
			.Configure<ForwardedHeadersOptions>(options => {
				options.ForwardedHeaders = forwardedHeaders;
			});

		// Allow more time to shutdown
		if (shutdownTimeoutMinutes > 0) {
			this.Services
				.Configure<HostOptions>(options => {
					options.ShutdownTimeout = TimeSpan.FromMinutes(shutdownTimeoutMinutes);
				});
		}

		// ******************************************************************************
		// Add Global Services
		//

		// Domain Environment
		this._innerBuilder.Services.AddSingleton<IDomainEnvironment, DomainEnvironment>();

		// Default Request timeouts
		this.Services.AddRequestTimeouts(options => {
			options.DefaultPolicy =
				new RequestTimeoutPolicy { Timeout = TimeSpan.FromSeconds(20) };
		});

		// Default Output Caching
		this.Services.AddOutputCache();

		// Default Memory Caching
		this.Services.AddMemoryCache(options => {
			options.TrackStatistics = !this.Environment.IsProduction();
		});


		// ******************************************************************************
		// Configure CORS
		//
		this.ConfigureCors();


		// ******************************************************************************
		// Open Telemetry
		//
		this.ConfigureTelemetry();


		// ******************************************************************************
		// Core (Infrastructure) Services
		//
		this.Services
			.AddGlobalExceptionHandling()
			.AddCoreServices()
			.AddDefaultHealthChecks();

	}

	void IHostApplicationBuilder.ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure) {
		((IHostApplicationBuilder)this._innerBuilder).ConfigureContainer(factory, configure);
	}

	/// <inheritdoc/>
	public IDomainApplicationBuilder ConfigureConductor(Action<ConductorOptionsBuilder> configure) {
		ArgumentNullException.ThrowIfNull(configure);

		var previousConfig = _conductorConfiguration;
		_conductorConfiguration = options => {
			previousConfig?.Invoke(options);
			configure(options);
		};

		return this;
	}


	/// <summary>
	/// Builds the application after registering and configuring domain services including
	/// authorization evaluators, documenters, and CQRS features.
	/// </summary>
	/// <returns>
	/// A configured <see cref="DomainApplication"/> instance ready to be run.
	/// </returns>
	/// <remarks>
	/// This method registers core domain services without specifying additional assemblies
	/// that should be scanned. If your domain services are in separate assemblies, consider using 
	/// the overloads that allow you to specify assemblies to scan.
	/// </remarks>
	public DomainApplication Build() {

		// Build the app!
		return this.BuildDomainCore();

	}

	/// <summary>
	/// Builds the application after registering and configuring domain services with custom
	/// assembly configuration.
	/// </summary>
	/// <param name="configureDomainServices">A callback to configure domain service assemblies.</param>
	/// <returns>
	/// A configured <see cref="DomainApplication"/> instance ready to be run.
	/// </returns>
	/// <remarks>
	/// This method allows you to specify additional assemblies that should be scanned for domain services,
	/// validators, and authorization handlers. Use the provided <see cref="DomainServicesBuilder"/>
	/// to register assemblies containing your domain components.
	/// </remarks>
	/// <example>
	/// <code>
	/// var app = builder.Build(domain => {
	///     domain.AddAssemblyContaining&lt;Asm1.GetOrders&gt;()
	///           .AddAssemblyContaining&lt;Asm2.GetUsers&gt;();
	/// });
	/// </code>
	/// </example>
	public DomainApplication Build(Action<DomainServicesBuilder> configureDomainServices) {

		// Build domain services if any...
		var domainBuilder = new DomainServicesBuilder();
		configureDomainServices(domainBuilder);

		return this.Build();
	}

	/// <summary>
	/// Builds the application after registering and configuring domain services, including
	/// the assembly containing the specified marker type.
	/// </summary>
	/// <typeparam name="TDomainMarker">A type from the assembly containing domain services to register.</typeparam>
	/// <returns>
	/// A configured <see cref="DomainApplication"/> instance ready to be run.
	/// </returns>
	/// <remarks>
	/// This is a convenience method that allows you to include an additional assembly containing the specified type.
	/// Use this method when your domain services are in a single separate assembly from your API.
	/// </remarks>
	/// <example>
	/// <code>
	/// var app = builder.Build&lt;SomeDomainType&gt;();
	/// </code>
	/// </example>
	public DomainApplication Build<TDomainMarker>() {
		return this.Build(domain => domain.AddAssemblyContaining<TDomainMarker>());
	}

	private DomainApplication BuildDomainCore() {

		// ******************************************************************************
		// Initializers
		//
		this.Services.AddApplicationInitializers();


		// ******************************************************************************
		// Authorization Services
		//
		this.Services.AddDefaultAuthorizationEvaluator();
		this.Services.AddDefaultAuthorizationDocumenter();


		// ******************************************************************************
		// App Domain - Conductor/FluentValidation/FluentAuthorization
		// If ConfigureConductor wasn't called, attempt to auto-bind from appsettings
		var conductorConfig = _conductorConfiguration ?? (options => options.BindConfiguration(this.Configuration));
		this.Services.AddDomainServices(conductorConfig);


		// ******************************************************************************
		// Ensure no build issues
		//
		ValidateDeferredLogs();


		// Build the app!
		return new(this._innerBuilder.Build());

	}


}