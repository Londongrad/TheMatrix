# Internal JWT Key Rotation

This runbook covers both internal token families:

- `InternalUserContextJwt`
- `InternalServiceJwt`

Each family now supports:

- `CurrentKeyId`
- `Keys`
- JWT header `kid`
- validation against all configured keys

## Desired steady state

Use at least two keys during rotation windows:

```json
"InternalServiceJwt": {
  "Issuer": "matrix.internal",
  "Audience": "matrix.services",
  "CurrentKeyId": "2026-04-primary",
  "Keys": {
    "2026-04-primary": "...current-secret...",
    "2026-03-previous": "...previous-secret..."
  },
  "LifetimeSeconds": 60
}
```

If a service still uses legacy single-key config:

```json
"InternalServiceJwt": {
  "Issuer": "...",
  "Audience": "...",
  "SigningKey": "..."
}
```

the readiness health check will report `Degraded`.

## Safe rotation steps

1. Add the new key to `Keys` on every validator and issuer, but keep the old `CurrentKeyId`.
2. Deploy that config everywhere.
3. Flip `CurrentKeyId` to the new key on issuers.
4. Wait at least `max token lifetime + clock skew`.
5. Remove the old key from `Keys`.

For this repository that usually means:

1. Update `InternalUserContextJwt` on:
   - `Matrix.ApiGateway`
   - every internal service API that validates gateway-issued internal tokens
2. Update `InternalServiceJwt` on:
   - internal service APIs that validate machine tokens
   - services that issue machine tokens through `InternalServiceJwtIssuer`

## Health signal

`/health/ready` now reports:

- `Healthy` when the current key is configured through `CurrentKeyId + Keys` and at least one overlap key exists
- `Degraded` when the service still uses legacy `SigningKey`
- `Degraded` when only one active key exists and there is no overlap window yet

This is intentional: rotation-unready config should stay visible without taking the service fully down.

## Emergency rollback

If a newly promoted key causes auth failures:

1. put the previous key back into `Keys` everywhere,
2. point `CurrentKeyId` back to the previous key on issuers,
3. redeploy issuers first, then validators if needed.

As long as validators still trust both keys, rollback is only a config change.
