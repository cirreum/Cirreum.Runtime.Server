# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.3.0] - 2026-08-20

### Added

- **WebApp antiforgery.** A `DomainRuntimeType.WebApp` runtime — the only posture whose
  authentication rides an ambient credential (the cookie session) — now gets CSRF protection
  from the runtime: antiforgery services are registered at builder construction with the
  request-token header set to `DomainAntiforgeryDefaults.HeaderName` (`X-CSRF-TOKEN`), and
  `UseDefaultMiddleware()` places `UseAntiforgery()` after authorization, where tokens bind to
  the resolved identity. `MapDefaultAntiforgeryToken()` maps an authenticated, WebApp-only
  endpoint (default `/_cirreum/antiforgery/token`, excluded from OpenAPI like the other
  framework routes) that stores the antiforgery cookie and returns the request token for
  browser clients to echo in the header.
- `DomainAntiforgeryDefaults` — the framework route prefix, header name, and default token
  endpoint route.

### Changed

- `UseDefaultMiddleware()` states its posture per runtime type: `WebApi` targets stateless,
  bearer- and machine-authenticated APIs and includes no antiforgery middleware — form-binding
  endpoints with no ambient credential opt out per endpoint with `DisableAntiforgery`. The
  middleware-order reference now points at the minimal-APIs article rather than the web-apps
  one.

### Updated

- Updated NuGet packages (`Cirreum.Services.Server` 1.5.0 — the attribute-authority consumer
  side: subject-kind resolution, effective-scheme dispatch, fill-only app-name fallback).

## [1.2.0] - 2026-08-04

### Added

- **The application-user bootstrap endpoint** (`GET /_cirreum/application-user`, the route
  constant shipped in `Cirreum.Domain` 4.2.0). Mapped automatically at `Build()` when the
  service collection contains an `IApplicationUserResolver` registration — no `Map*` call for
  an app to forget. The endpoint requires authentication and nothing else: it is never
  dispatched through Conductor, so no authorization gate stands between a disabled caller and
  the record describing their state — the property that lets a WebAssembly client render
  `ViewState.Disabled` for the first time. It reads server-resolved user state (accepting
  nothing from the request), serializes the app's own user type against its runtime type, and
  returns `204` for a caller with no record.

### Fixed

- **`DomainApplicationBuilder`'s class-level example compiled against an API that does not
  exist**: `MapEndpoints()` and `InitializeAndRunAsync()` are not members of
  `DomainApplication` (the surface is `MapApiEndpoints(...)` and `RunAsync()`), and the
  example's `using var` could never compile — the type is `IAsyncDisposable` only, so it
  needs `await using`. `DomainApplication`'s class documentation also now describes the type
  itself (the built application wrapper and its compose-map-run flow) rather than only its
  static factory.

### Updated

- Re-pinned `Cirreum.Services.Server` `1.4.6` → `1.4.7`, `Cirreum.Logging.Deferred` `1.0.116`
  → `1.0.117`, `Cirreum.Cors` `1.0.108` → `1.0.109` (Cirreum spine 4.2.0 wave; carries
  `Cirreum.Domain` 4.2.0 with the shared route constant).

## [1.1.15] - 2026-07-31

### Updated

- Updated NuGet packages (Cirreum spine 4.0.1 wave: `Cirreum.Domain` 4.0.1 / `Cirreum.AuthenticationProvider` 2.0.3 / `Cirreum.Services.*` repins).

## [1.1.14] - 2026-07-30

### Updated

- Updated NuGet packages — picks up the `Cirreum.Domain` 3.0.0 authorization-enforcement wave
  (fail-open operation-authorization fix + `IPolicyAuthorizer` rename) through the re-pinned
  `Cirreum.Services.Server` 1.4.5; see Cirreum.Domain `MIGRATION-v3.md`.

## [1.1.12] - 2026-07-27

### Updated

- Updated NuGet packages.

## [1.1.11] - 2026-07-24

### Updated

- Updated NuGet packages.

## [1.1.10] - 2026-07-20

### Fixed

- The `Build()`-time registration of the default `IAuthenticationBoundaryResolver`
  now `TryAdd`s the Kernel default (`Cirreum.Security`) directly — the
  `AddDefaultAuthenticationBoundaryResolver` extension it previously called was
  removed with the seam's relocation to `Cirreum.Kernel` (ADR-0032). Semantics are
  unchanged and deliberate: the registration runs at `Build()`, after the
  application's composition, so a scheme-aware resolver registered by the
  Authentication track (primary scheme → `Global`, other authenticated schemes →
  `Tenant`) or an app-registered custom resolver always wins.

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

