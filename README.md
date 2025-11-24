# Cirreum.Runtime.Server

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Runtime.Server.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Runtime.Server/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Runtime.Server.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Runtime.Server/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Runtime.Server?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Runtime.Server/releases)
[![License](https://img.shields.io/github/license/cirreum/Cirreum.Runtime.Server?style=flat-square&labelColor=1F1F1F&color=F2F2F2)](https://github.com/cirreum/Cirreum.Runtime.Server/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**Foundation runtime for building domain-driven ASP.NET Core applications**

## Overview

**Cirreum.Runtime.Server** is a foundation library that provides a structured runtime environment for ASP.NET Core web applications. It offers pre-configured middleware pipelines, built-in observability, authentication support, and a fluent builder pattern for creating domain-driven applications.

## Key Features

- **Simplified Application Bootstrap** - Fluent builder pattern for configuring ASP.NET Core applications with sensible defaults
- **Built-in Observability** - OpenTelemetry integration with Azure Monitor and OTLP exporter support
- **Health Check Endpoints** - Pre-configured startup, liveness, readiness, and internal health checks
- **Authentication Ready** - Microsoft Identity Web integration for authentication and authorization
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

## Configuration

The runtime supports configuration through appsettings.json and environment variables:

```json
{
  "Cirreum": {
    "Runtime": "WebApi",
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

{REPO-NAME} follows [Semantic Versioning](https://semver.org/):

- **Major** - Breaking API changes
- **Minor** - New features, backward compatible
- **Patch** - Bug fixes, backward compatible

Given its foundational role, major version bumps are rare and carefully considered.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Cirreum Foundation Framework**  
*Layered simplicity for modern .NET*