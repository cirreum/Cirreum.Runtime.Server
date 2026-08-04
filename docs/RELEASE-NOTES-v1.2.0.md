# Cirreum.Runtime.Server 1.2.0 — the application-user bootstrap endpoint

## Why this release exists

A WebAssembly client has never been able to tell a user they are disabled, because reading the
record that says so required not being disabled: the client resolved its application user
through its own authorizable operations, the disabled gate denied them, and the router fell
through to the "not provisioned" screen with an error toast on top. The durable principle: **a
bootstrap path must not depend on the system it bootstraps.** The server has always had this
property by structure — `UserStateAccessor` resolves the application user outside the
dispatcher; the client's path violated it by convenience.

This release gives the client a server-side path with the same property.

## What's new

**`GET /_cirreum/application-user`** — mapped automatically at `Build()` when the service
collection contains an `IApplicationUserResolver` registration (the one apps already make via
`CirreumAuthenticationBuilder.AddApplicationUserResolver<T>()`; no new registration, no `Map*`
call to forget). The `/_cirreum/` prefix is the framework's reserved route namespace, shared
with the client via `ApplicationUserEndpoint.Route` in `Cirreum.Domain` so the two ends cannot
drift.

The endpoint's contract:

- **Requires authentication and nothing else.** It is never dispatched through Conductor, so
  no authorization gate — including the disabled-user gate — stands between a caller and their
  own record. No exemption is introduced anywhere: the gate stays absolute for operations.
- **Accepts nothing from the request.** It reads `IUserStateAccessor` state, resolved before
  any endpoint runs — a parameter taking an external user id would let any authenticated
  caller read any user's record and roles.
- **Costs nothing.** The record is already resolved per-request by the claims transformer or
  the accessor's cache-miss path; the endpoint is a projection of work already paid for.
- **Returns the app's own type**, serialized against its runtime type — serializing against
  the declared `IApplicationUser` would silently drop every app-defined field behind a valid
  200. A record-less caller gets `204`, so clients distinguish "no record" from "a record"
  without inspecting a body.

## Coordinated downstream work

`Cirreum.Runtime.Wasm` 2.0.0 replaces the client-side resolver registration with
`AddApplicationUser<TUser>(Uri)` calling this endpoint; the `Msal` / `Oidc` wrappers follow.

## Compatibility

Fully additive. Apps with no `IApplicationUserResolver` registration get no endpoint. Note
that the server-side resolver's return type becomes a client-facing contract once WASM clients
call the endpoint — fields not intended for a browser now reach one if the resolver returns
them.

## See also

- `docs/CHANGELOG.md` — the enumerated changes
- `Cirreum.Domain` 4.2.0 release notes — the shared route constant
