# Cirreum.Runtime.Server

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Runtime.Server.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Runtime.Server/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Runtime.Server.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Runtime.Server/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Runtime.Server?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Runtime.Server/releases)
[![License](https://img.shields.io/badge/license-MIT-F2F2F2?style=flat-square&labelColor=1F1F1F)](https://github.com/cirreum/Cirreum.Runtime.Server/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**Foundation runtime for building domain-driven ASP.NET Core applications**

## Overview

**Cirreum.Runtime.Server** is a foundation library that provides a structured runtime environment for ASP.NET Core web applications. It offers pre-configured middleware pipelines, built-in observability, authentication support, and a fluent builder pattern for creating domain-driven applications.

## Key Features

- **Simplified Application Bootstrap** - Fluent builder pattern for configuring ASP.NET Core applications with sensible defaults
- **Default Middleware Pipeline** - `UseDefaultMiddleware()` wires the canonical ordering for the declared runtime posture (`WebApi` or `WebApp`), including the HTTP→`IInvocationContext` bridge for the unified inbound seam
- **WebApp Antiforgery** - `DomainRuntimeType.WebApp` runtimes register antiforgery services, run `UseAntiforgery()` after authorization, and can map an authenticated token endpoint (`MapDefaultAntiforgeryToken()`) for browser clients
- **Built-in Observability** - OpenTelemetry integration with Azure Monitor and OTLP exporter support
- **Health Check Endpoints** - Pre-configured startup, liveness, readiness, and internal health checks
- **Framework Endpoints** - the application-user bootstrap endpoint (`/_cirreum/application-user`) mapped automatically when an `IApplicationUserResolver` is registered
- **CORS Support** - Configurable cross-origin resource sharing with environment-based settings
- **Deferred Logging** - Optimized startup logging that captures and replays logs after initialization

## Getting Started

```csharp
using Cirreum.Runtime;

// Create a domain application builder
var builder = DomainApplication.CreateBuilder(args);

// Build the application with domain service assemblies
using var app = builder.Build<MyDomainAssembly>();

// Use the default middleware pipeline
app.UseDefaultMiddleware();

// Map your endpoints
app.MapGet("/", () => "Hello World!");

// Run the application
await app.RunAsync();
```

## Default Middleware Pipeline

`app.UseDefaultMiddleware()` configures the pipeline in this order:

```csharp
app
    .UseExceptionHandler()
    .UseForwardedHeaders()
    .UseStaticFiles()
    .UseRouting()
    .UseRequestTimeouts()
    .UseConfiguredCors()
    .UseAuthentication()
    .UseAuthorization();

// WebApp runtimes only — tokens bind to the resolved identity
app.UseAntiforgery();

app
    .UseInvocationContext()    // HTTP→IInvocationContext bridge
    .UseOutputCache();
```

`UseInvocationContext()` runs late on purpose — after authentication and authorization complete — so the snapshotted `IInvocationContext.User` reflects the fully-resolved authenticated principal. Framework-internal code (`UserStateAccessor`, the conductor pipeline, authorizers, audit) then reads identity through the unified seam regardless of transport.

The pipeline varies by the declared runtime posture (`Cirreum:Runtime`): **`WebApi`** targets stateless, bearer- and machine-authenticated APIs with no ambient credential, so no antiforgery middleware is included — an endpoint that binds form data (`[FromForm]`, `IFormFile`) opts out per endpoint with `DisableAntiforgery`. **`WebApp`** authenticates browsers with a cookie session — an ambient credential — so the runtime registers antiforgery (request-token header `X-CSRF-TOKEN`) and places `UseAntiforgery()` after authorization. `MapDefaultAntiforgeryToken()` adds an authenticated endpoint (default `/_cirreum/antiforgery/token`) that stores the antiforgery cookie and returns the request token.

**Not included by design:** Response Compression (handle at proxy/CDN), Response Caching (superseded by Output Caching), Rate Limiting (configure per requirements), Sessions (Cirreum applications are expected to remain stateless).

## Configuration

The runtime supports configuration through appsettings.json and environment variables:

```json
{
  "Cirreum": {
    "Runtime": "WebApi",   // or "WebApp" — declares the posture the pipeline configures for
    "Diagnostics": {
      "EnableTelemetry": true,
      "EnableMetrics": true,
      "EnableTracing": true,
      "SamplingRatio": 1.0,
      "AzureMonitor": {
        "ConnectionString": "InstrumentationKey=..."
      }
    },
    "LandingPage": "/health/startup"
  }
}
```


## Contribution Guidelines

1. **Be conservative with new abstractions**  
   The API surface must remain stable and meaningful.

2. **Limit dependency expansion**  
   Only add foundational, version-stable dependencies.

3. **Favor additive, non-breaking changes**  
   Breaking changes ripple through the entire ecosystem.

4. **Include thorough unit tests**  
   All primitives and patterns should be independently testable.

5. **Document architectural decisions**  
   Context and reasoning should be clear for future maintainers.

6. **Follow .NET conventions**  
   Use established patterns from Microsoft.Extensions.* libraries.

## Versioning

Cirreum.Runtime.Server follows [Semantic Versioning](https://semver.org/):

- **Major** - Breaking API changes
- **Minor** - New features, backward compatible
- **Patch** - Bug fixes, backward compatible

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Cirreum Foundation Framework**  
*Layered simplicity for modern .NET*