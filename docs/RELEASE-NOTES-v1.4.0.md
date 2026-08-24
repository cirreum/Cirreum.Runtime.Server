# Cirreum.Runtime.Server 1.4.0 — a browser can authenticate its WebSocket

## Why this release exists

A browser cannot set request headers on a WebSocket upgrade. The API gives it a URL and a
subprotocol list and nothing else, so a client authenticating a long-lived connection sends
its bearer token as an `access_token` query parameter — the convention SignalR's own clients
follow, and the only one available to them.

No Cirreum scheme read a credential from there. The upgrade was refused, and the failure
concealed itself: SignalR does not surface a rejected WebSocket upgrade as an error, it
negotiates down to Server-Sent Events or long polling. The application kept working. It never
used WebSockets, nothing reported the downgrade, and the configuration looked correct because
in every respect except the one that mattered it was.

`Cirreum.Services.Server` 1.6.0 shipped the middleware that closes this. This release puts it
in the pipeline, which is what makes it true for an application that did not go looking.

## What's new

**`UseDefaultMiddleware()` calls `UseConnectionCredential()`**, between CORS and
authentication:

```csharp
app
    .UseRequestTimeouts()
    .UseConfiguredCors()
    .UseConnectionCredential()
    .UseAuthentication()
    .UseAuthorization();
```

Both neighbours are load-bearing. Routing must already have resolved the endpoint, because
the endpoint is what scopes the promotion; and the credential must reach the `Authorization`
header before any scheme reads it.

The middleware promotes the query-carried credential into that header, so every authentication
scheme and every scheme selector resolves it from the one place they have always read it. None
of them need to know this case exists — including schemes written after this release, and the
JWT audience-routing selector in `Cirreum.Runtime.Authentication`, which sits outside the
scheme packages entirely. The value is promoted verbatim, so a scheme prefix carried inside
the credential continues to route dispatch.

**The promotion is scoped to where a client has no alternative.** It applies only when the
resolved endpoint is a SignalR hub or carries `InvocationConnectionMetadata`, and only when
the request has no `Authorization` header of its own — a header that is present wins, whatever
scheme it names. A stray `?access_token=` on an ordinary API route is left alone and carries
no authority.

## Compatibility

- **No change to any request that already worked.** The middleware acts only on connection
  endpoints, and only on requests carrying no `Authorization` header — the requests that were
  failing the upgrade.
- **Applications composing their own pipeline** call `UseConnectionCredential()` themselves,
  in the same position.
- **A WebSocket upgrade that previously fell back may now succeed**, which is the point. An
  application that had come to depend on the SSE fallback's behaviour — server-push ordering,
  proxy timeouts tuned to long polling — is now genuinely on WebSockets.

## See also

- `Cirreum.Services.Server` 1.6.0 — the middleware itself, and the
  `InvocationConnectionMetadata` stamp on `MapWebSocketHandler`.
- `Cirreum.RemoteConnections.SignalR` and `Cirreum.RemoteConnections.WebSockets` 1.0.0 — the
  caller-side connections that present the credential this release accepts.
