# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.9] - 2026-07-19

### Updated

- Updated NuGet packages.

## [1.1.8] - 2026-07-11

### Updated

- Updated NuGet packages (`OpenTelemetry.Instrumentation.Runtime` 1.15.1 → 1.16.0).

## [1.1.7] - 2026-07-08

### Updated

- Updated NuGet packages as part of the lower-layer changes.

## [1.1.6] - 2026-07-06

### Fixed

- Re-pinned `Cirreum.Services.Server` from a never-published `2.0.0` (a local-feed smoke-test version) to the published `1.3.0`, which carries the connection registry, the auth-event connection-termination handler, and `CirreumUserIdProvider` wired inside `AddCoreServices()` (ADR-0027 Phase B). The stale pin made the `v1.1.5` publish fail at restore — **no NuGet artifact ever landed for 1.1.5**; this release is the first published since 1.1.4.

### Updated

- Updated NuGet packages.

## [1.1.5] - 2026-07-04

### Updated

- Updated NuGet packages.

## [1.1.4] - 2026-05-10

### Updated

- Updated NuGet packages.

## [1.1.3] - 2026-05-10

### Updated

- Updated NuGet packages.

## [1.1.2] - 2026-05-09

### Updated

- Updated NuGet packages.

## [1.1.1] - 2026-05-09

### Updated

- Updated NuGet packages.

## [1.1.0] - 2026-05-07

### Added

- **`UseDefaultMiddleware()` now wires `app.UseInvocationContext()`** between `UseAuthorization()` and `UseOutputCache()`. Apps using the default pipeline pick up the HTTP→`IInvocationContext` bridge automatically with no code change. Placement is the canonical late-spot — after authentication and authorization complete, before endpoint execution — so the snapshotted invocation reflects the fully-resolved authenticated principal.

### Changed

- **Package dependency graph** — `Microsoft.Identity.Web` is no longer a direct package reference. The library was not used anywhere in Runtime.Server's source. Auth tracks that need it (`Cirreum.Authorization.Entra`, `Cirreum.Authorization.External`) reference it explicitly themselves; apps using those tracks via `Cirreum.Runtime.Authorization` pick it up transitively as before — no action needed. Apps that were calling `Microsoft.Identity.Web` APIs directly without going through the Cirreum auth track should add an explicit `<PackageReference Include="Microsoft.Identity.Web" />` to their project.

  Side-benefit: dropping this unused reference also resolves a build-time `CS0433 'DefaultAzureCredential' exists in both Azure.Core and Azure.Identity` ambiguity that surfaced when `Azure.Monitor.OpenTelemetry.AspNetCore 1.5.0` brought in `Azure.Core 1.54.0`. The conflict was rooted in `Microsoft.Identity.Web 4.8.0` floor-pinning an older `Azure.Identity 1.17.x` that lacked the `[TypeForwardedTo]` to Azure.Core's copy of `DefaultAzureCredential`. With the floor pin gone, NuGet resolves Azure.Identity to a current, type-forwarder-bearing version transitively from `Azure.Monitor.OpenTelemetry.AspNetCore 1.5.0`.

### Updated

- **`Cirreum.Services.Server`** — `1.1.0` → `1.2.0`. Picks up the `UseInvocationContext()` extension and the `IInvocationContextAccessor` registration in `AddCoreServices()`.
- **`Azure.Monitor.OpenTelemetry.AspNetCore`** — `1.4.0` → `1.5.0`.

### Migration

No code changes required for apps using `UseDefaultMiddleware()` — the bridge is wired automatically on package update. Apps that compose their pipeline manually need to add `app.UseInvocationContext()` after `UseAuthorization()` themselves (see `Cirreum.Services.Server` 1.2.0 release notes for guidance).

This release lights up the unified inbound seam from `Cirreum.InvocationProvider 1.0.1` end-to-end for HTTP — `UserStateAccessor`, the conductor pipeline, authorizers, and audit now read identity through `IInvocationContextAccessor` instead of `IHttpContextAccessor` directly. `IHttpContextAccessor` remains available for app code that needs HTTP-specific concerns (response headers, cookies).

## [1.0.49] - 2026-05-01

### Updated
- Updated NuGet packages.

