# Cirreum.Runtime.Server 1.3.0 — the pipeline follows the declared posture

## Why this release exists

`UseDefaultMiddleware()` has always shipped the Web API posture: stateless endpoints
authenticated by bearer tokens and machine credentials — schemes a browser never attaches on
its own, so cross-site request forgery has nothing to ride. The runtime also supports
`DomainRuntimeType.WebApp`, whose authentication is a cookie session — the one ambient
credential in the framework's scheme inventory, and therefore the one place CSRF exists.
Until now the runtime was silent about that difference: a WebApp host got the same pipeline
as an API host, and antiforgery was left as an exercise nobody was told to do.

The runtime already holds the deciding fact — the application declares its posture at
bootstrap (`Cirreum:Runtime`). This release makes the pipeline read it.

## What's new

**WebApp runtimes get antiforgery from the runtime.** At builder construction, a
`DomainRuntimeType.WebApp` host registers antiforgery services with the request-token header
set to `DomainAntiforgeryDefaults.HeaderName` (`X-CSRF-TOKEN`). In `UseDefaultMiddleware()`,
`UseAntiforgery()` runs after authentication and authorization — antiforgery tokens bind to
the resolved identity — and before the invocation-context bridge. Web API hosts are
untouched: no services, no middleware, no behavioral change.

**A token endpoint for browser clients.** `MapDefaultAntiforgeryToken()` maps an
authenticated GET (default `/_cirreum/antiforgery/token`, excluded from OpenAPI like the
other framework routes; WebApp-only, throws elsewhere) that stores the antiforgery cookie
and returns the request token:

```csharp
var response = await http.GetFromJsonAsync<TokenResponse>("/_cirreum/antiforgery/token");
http.DefaultRequestHeaders.Add("X-CSRF-TOKEN", response.Token);
```

**The posture is documented where it's decided.** `UseDefaultMiddleware()`'s docs now state
the split: Web API endpoints that bind form data (`[FromForm]`, `IFormFile`) with no ambient
credential opt out per endpoint with `DisableAntiforgery` — the .NET 8+ form-binding rule
forces that choice loudly either way, and opting out is the correct answer when no cookie
authenticates the caller.

## Compatibility

- **Web API runtimes: no change.** The pipeline is byte-for-byte what 1.2.0 configured.
- **WebApp runtimes: antiforgery becomes active.** Form-binding endpoints acquire token
  validation (401-adjacent 400s for requests without the header token); endpoints that
  should not validate can opt out per endpoint. Previously such endpoints could not have
  worked at all without the app registering antiforgery itself — .NET requires the
  middleware wherever form binding occurs — so this activates protection rather than
  breaking working code.
- Rides along: `Cirreum.Services.Server` 1.5.0 (attribute-authority consumer side —
  subject-kind resolution, effective-scheme dispatch, fill-only app-name fallback).

## See also

- `Cirreum.Services.Server 1.5.0` — the user-state assembly this runtime hosts.
- `Cirreum.Runtime.Authentication` — composes the WebApp cookie flows whose ambient
  credential this release protects; its backlog tracks a declaration-driven advisory for
  hosts whose scheme composition and declared posture disagree.
