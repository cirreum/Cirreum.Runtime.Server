# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Cirreum.Runtime.Server** is a .NET 10.0 foundation library for building domain-driven ASP.NET Core web applications. It provides a structured runtime environment with built-in support for observability, authentication, and common middleware patterns.

## Common Development Commands

### Build Commands
```bash
# Build the project
dotnet build

# Build in release mode
dotnet build -c Release

# Clean build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore
```

### Package Management
```bash
# Pack NuGet package (local development)
dotnet pack -c Release

# Pack with specific version
dotnet pack -c Release -p:VersionPrefix=1.0.100 -p:VersionSuffix=rc
```

### Development Workflow
```bash
# Build and watch for changes (if implementing in consumer projects)
dotnet watch build

# Format code
dotnet format
```

## High-Level Architecture

### Core Components

1. **DomainApplication** (`DomainApplication.cs`)
   - Main application wrapper around WebApplication
   - Provides default middleware pipeline for stateless APIs
   - Configurable health endpoints at `/health/*` (startup, liveness, readiness, internal)
   - Built-in exception handling, CORS, authentication, and caching support

2. **DomainApplicationBuilder** (`DomainApplicationBuilder.cs`)
   - Fluent builder for configuring domain applications
   - Integrates OpenTelemetry (metrics, tracing, logging) with configurable sampling
   - Azure Monitor and OTLP exporter support
   - Assembly scanning for domain services via `Build<TAssembly>()` methods

3. **Observability Infrastructure**
   - OpenTelemetry with environment-based configuration
   - Azure Monitor integration with DefaultAzureCredential
   - Deferred logging for startup optimization
   - Configurable via `DiagnosticsOptions` in appsettings.json

### Configuration Patterns

The library uses hierarchical configuration with environment variable overrides:
- `Cirreum:Diagnostics:*` - Telemetry and monitoring settings
- `Cirreum:LandingPage` - Custom root path redirect
- Environment variables use underscore format: `Cirreum_LANDING_PAGE`

### Extension Points

When extending this library:
1. Follow the builder pattern established by `DomainApplicationBuilder`
2. Use the existing options pattern (e.g., `DiagnosticsOptions`)
3. Leverage the global using directives for common namespaces
4. Maintain compatibility with the Microsoft.Extensions.* ecosystem

### Key Dependencies

- **Microsoft.Identity.Web** - Authentication and authorization
- **Azure.Monitor.OpenTelemetry.AspNetCore** - Azure monitoring
- **OpenTelemetry** packages - Observability
- **Cirreum** packages - Foundation framework components

The library is designed as a stable foundation with careful API evolution. Breaking changes are rare and impact the entire Cirreum ecosystem.