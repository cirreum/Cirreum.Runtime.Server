# Cirreum.Runtime.Server 1.1.0 — Default pipeline picks up the IInvocationContext bridge

`UseDefaultMiddleware()` now wires `app.UseInvocationContext()` automatically between `UseAuthorization()` and `UseOutputCache()`. Apps that go through `Build()` + `UseDefaultMiddleware()` pick up the unified HTTP→`IInvocationContext` seam with zero code change on package update.

Strictly additive. No public API changes. No behavior change for apps composing the pipeline manually (they already had to wire `UseInvocationContext()` themselves per the `Cirreum.Services.Server` 1.2.0 release notes; this release does not affect them).

---

## Why this release exists

`Cirreum.Services.Server` 1.2.0 added `UseInvocationContext()` — the HTTP→`IInvocationContext` bridge middleware — as a public extension on `IApplicationBuilder`. Apps composing their pipeline manually pick it up by adding the call themselves; apps using this runtime's `UseDefaultMiddleware()` need the runtime to wire it for them. This release does that.

After this lands, every Cirreum app on `Runtime.Server 1.1.0+` has the unified inbound seam lit up automatically. Framework-internal code (`UserStateAccessor`, the conductor pipeline, authorizers, audit) reads identity through `IInvocationContextAccessor.Current` instead of `IHttpContextAccessor.HttpContext` directly. The migration is invisible to existing apps.

---

## What changed

The fluent chain inside `UseDefaultMiddleware()`:

```diff
  this
      .UseExceptionHandler()
      .UseForwardedHeaders()
      .UseStaticFiles()
      .UseRouting()
      .UseRequestTimeouts()
      .UseConfiguredCors()
      .UseAuthentication()
      .UseAuthorization()
+     .UseInvocationContext()
      .UseOutputCache();
```

XML doc `<remarks>` enumeration updated to match.

Placement rationale: the snapshotted `IInvocationContext.User` must reflect the fully-resolved authenticated principal, so the bridge runs *after* `UseAuthentication()` / `UseAuthorization()` and *before* endpoint execution. See `Cirreum.Services.Server 1.2.0` release notes for the full design rationale.

---

## Updated dependencies

- **`Cirreum.Services.Server`** — `1.1.0` → `1.2.0`. Brings the `UseInvocationContext()` extension and the `IInvocationContextAccessor` registration in `AddCoreServices()`.
- **`Azure.Monitor.OpenTelemetry.AspNetCore`** — `1.4.0` → `1.5.0`.

## Dependency graph cleanup

- **`Microsoft.Identity.Web`** — no longer a direct package reference. The library was not used anywhere in Runtime.Server's source code; the reference appears to have been carried forward from earlier architecture and is now correctly pruned. This is a graph change, not an API change — Runtime.Server's public surface is unchanged.

### Migration

Apps using the **Cirreum auth tracks** (`Cirreum.Runtime.Authorization` with the Entra or External providers) get `Microsoft.Identity.Web` transitively through the auth-track packages, which reference it explicitly themselves. **No action needed.**

Apps using **OIDC, Descope, or EntraExternalId** auth tracks never needed Microsoft.Identity.Web. **No action needed.**

Apps that were calling `Microsoft.Identity.Web` APIs **directly** without going through the Cirreum auth track were relying on Runtime.Server's transitive — that's a poor dependency hygiene pattern, but if you're in this state, the fix is one line:

```bash
dotnet add package Microsoft.Identity.Web
```

### Side-benefit: dissolves a transitive Azure.Identity / Azure.Core type collision

`Microsoft.Identity.Web 4.8.0` floor-pinned an older `Azure.Identity 1.17.x` that lacked the `[TypeForwardedTo]` to Azure.Core's copy of `DefaultAzureCredential`. When `Azure.Monitor.OpenTelemetry.AspNetCore 1.5.0` brought in `Azure.Core 1.54.0` (which also exports the type), the build failed with `CS0433: 'DefaultAzureCredential' exists in both Azure.Core and Azure.Identity`.

With the unused `Microsoft.Identity.Web` reference removed, the floor pin is gone and NuGet resolves Azure.Identity to a current, type-forwarder-bearing version transitively from `Azure.Monitor.OpenTelemetry.AspNetCore 1.5.0`. The two copies of `DefaultAzureCredential` collapse onto one canonical definition and the build is clean.

This is the right kind of fix — addressing the root cause (a stale unused dependency) rather than papering over the symptom (pinning Azure.Identity explicitly).

---

## What this enables

This release is #7 in the Invocation family rollout. With it landed, the foundation (#1–#7) is complete for HTTP:

- ✓ `Cirreum.Providers 1.1.0` — `ProviderType.Invocation`
- ✓ `Cirreum.InvocationProvider 1.0.1` — server-side abstractions
- ✓ `Cirreum.Core 5.1.0` — client-side `IRemoteConnection` abstraction
- ✓ `Cirreum.Services.Server 1.2.0` — HTTP→`IInvocationContext` bridge
- ✓ `Cirreum.Runtime.Server 1.1.0` *(this release)* — auto-wires the bridge

Next: `Cirreum.Runtime.AuthorizationProvider` (patch — `Items` reads switch from `IHttpContextAccessor` to `IInvocationContextAccessor`), then per-source adapters (SignalR, WebSockets, gRPC) that populate the same seam from non-HTTP transports.

---

## Compatibility

- **Strictly additive.** No public API changed. `UseDefaultMiddleware()` signature unchanged; only the internal wiring grew by one middleware.
- **Source-compatible** with 1.0.x.
- **No new package dependencies** — Services.Server was already a transitive dependency.

---

## Migration

For apps using `UseDefaultMiddleware()`: nothing to do. Update the package, the bridge wires itself.

For apps composing the pipeline manually: nothing to do *here* — you should already be on the manual `app.UseInvocationContext()` path per the `Cirreum.Services.Server 1.2.0` release notes. This runtime release does not affect manually-composed pipelines.

---

## See also

- `CHANGELOG.md` — condensed change list for `1.1.0`.
- [`Cirreum.Services.Server 1.2.0`](https://www.nuget.org/packages/Cirreum.Services.Server) — the bridge middleware this release wires.
- [`Cirreum.InvocationProvider 1.0.1`](https://www.nuget.org/packages/Cirreum.InvocationProvider) — the abstractions the seam publishes.
