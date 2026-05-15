# Auth flow

Use in-process ASP.NET Core Identity for local username/password, plus a single generic external-login pipeline for OAuth/OIDC providers. We do NOT support an external OIDC provider (for now) and keep things self-implemented.

**Identity** is **only** "prove who signed in."  
**Tenancy** stays the source of truth for `persona` and effective `claims[]`.

## Token transport

For web clients, transfer **refresh token** in **HttpOnly cookie**, and **access token** in the body as usual.

For mobile clients, transfer both in the body.