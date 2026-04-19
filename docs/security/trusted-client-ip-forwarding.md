# Trusted client IP forwarding

This project no longer trusts raw `X-Real-IP` from clients.

The intended trust model is:

1. The edge proxy strips any client-supplied forwarding headers and writes its own.
2. `Matrix.ApiGateway` restores the real client IP through `UseForwardedHeaders` and an explicit trusted proxy list.
3. `Matrix.ApiGateway` passes the normalized client IP to `Identity` as trusted internal context.
4. `Identity` trusts that forwarded client IP only when the request is authenticated by the internal gateway key; otherwise it falls back to `RemoteIpAddress`.

## Local development

For direct local launches without nginx/traefik in front, keep forwarded header recovery disabled.

`src/gateways/Matrix.ApiGateway/appsettings.Development.json`

```json
{
  "TrustedForwardedHeaders": {
    "Enabled": false,
    "TrustLoopback": false,
    "ForwardLimit": 1,
    "KnownProxies": [],
    "KnownNetworks": []
  }
}
```

`src/services/identity/Matrix.Identity.Api/appsettings.Development.json`

```json
{
  "TrustedForwardedHeaders": {
    "Enabled": false,
    "TrustLoopback": false,
    "ForwardLimit": 1,
    "KnownProxies": [],
    "KnownNetworks": []
  }
}
```

## Recommended server rollout

If nginx is the public edge proxy in front of `Matrix.ApiGateway`, configure trusted forwarding on the gateway only.

### Gateway appsettings / environment

Example appsettings:

```json
{
  "TrustedForwardedHeaders": {
    "Enabled": true,
    "TrustLoopback": false,
    "ForwardLimit": 1,
    "KnownProxies": [ "127.0.0.1", "::1" ],
    "KnownNetworks": []
  }
}
```

Equivalent environment variables:

```text
TrustedForwardedHeaders__Enabled=true
TrustedForwardedHeaders__ForwardLimit=1
TrustedForwardedHeaders__TrustLoopback=false
TrustedForwardedHeaders__KnownProxies__0=127.0.0.1
TrustedForwardedHeaders__KnownProxies__1=::1
```

If nginx runs on another machine, replace the loopback entries with the real proxy IP addresses or trusted CIDR ranges.

### Identity appsettings / environment

If `Identity` is reachable only from the gateway and not behind a public reverse proxy, keep forwarded header recovery disabled there:

```json
{
  "TrustedForwardedHeaders": {
    "Enabled": false
  }
}
```

If `Identity` is also placed behind its own reverse proxy and must recover remote IPs outside the gateway flow, configure a separate trusted proxy list for that deployment.

## nginx edge example

The edge proxy should overwrite forwarding headers instead of trusting what the client sent:

```nginx
location / {
    proxy_pass https://gateway_upstream;
    proxy_set_header Host $host;
    proxy_set_header X-Forwarded-Proto $scheme;
    proxy_set_header X-Forwarded-Host $host;
    proxy_set_header X-Forwarded-For $remote_addr;
    proxy_set_header X-Real-IP $remote_addr;
}
```

## Operational guidance

- Configure `TrustedForwardedHeaders` on public ingress services only.
- Do not trust raw forwarding headers inside downstream business services.
- When in doubt, prefer exact `KnownProxies` entries over broad `KnownNetworks`.
- If the proxy chain changes, update the trusted proxy list before rollout.
