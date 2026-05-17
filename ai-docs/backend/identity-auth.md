# Identity & authentication

## Architectural split

| Concern | Owner |
|---------|--------|
| Prove who signed in (`Account`, sessions, tokens) | **Identity** BC (`identity` schema) |
| Org membership, effective `claims[]`, preset role labels | **Tenancy** BC |
| Listener profile | **Listener** BC |
| Platform operators | **Platform** BC |

Identity **does not** store org ACLs or persona permissions. It validates persona choice at token mint time via **read-model ports** (`ITenancyPersonaReadModel`, `IListenerPersonaReadModel`, `IPlatformPersonaReadModel` in `Amuse.Modules/Identity/Contracts/`).

Auth approach (see also [`ads/auth/auth-flow.md`](../../ads/auth/auth-flow.md)):

- **Local password:** in-process ASP.NET Core Identity (`ApplicationUser` in Identity schema).
- **External:** generic OAuth2/OIDC completion (`authorization_code` + PKCE or `id_token`); Amuse issues its own tokens (IdP tokens are not API bearers).
- **Refresh:** proves `Account` only; **persona is not stored on `refresh_session`** — client sends desired persona on each `POST /refresh`.

## HTTP endpoints

All routes are under **`/api/v1/identity`**.

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/login/password` | No | Email/password sign-in; returns tokens for requested persona |
| `POST` | `/external/complete` | No | Complete external OAuth/OIDC login |
| `POST` | `/refresh` | No | New access token (+ rotated refresh) for given persona; also used to **switch persona** |
| `POST` | `/revoke` | No | Revoke refresh session; optionally blacklist current access `jti` |
| `GET` | `/me` | Bearer | Current `Account` profile |
| `GET` | `/personas` | Bearer | List personas available to the signed-in account |

There is **no** separate `POST /personas/select` — persona switching is **`POST /refresh`** with a new `context`.

### Request validation

- DTOs use **domain enums** (e.g. `PersonaContextType`) serialized as **camelCase** JSON (`org`, `listener`, `platform`).
- **FluentValidation** validators live next to features (`*RequestValidator.cs`).
- **`RequestValidationFilter`** runs on login, external complete, and refresh routes (`.WithRequestValidation()`).
- Invalid input → `400` validation problem; business failures → `400` problem with `code` extension (see errors below).

### Persona context (request body)

```json
{
  "type": "platform",
  "orgId": null,
  "listenerId": null
}
```

| `type` | Required fields | Rules |
|--------|-----------------|--------|
| `org` | `orgId` (non-empty GUID) | Must be active org member (Tenancy read model) |
| `listener` | `listenerId` (non-empty GUID) | Profile must belong to account |
| `platform` | neither `orgId` nor `listenerId` | Account must be a platform operator |

Mapping: `PersonaContextMapper.ToDomain` → `Result<PersonaContext>` (no exceptions for bad input).

## Token transport

Header: **`X-Amuse-Client`**

| Client | Value | Refresh token | Access token |
|--------|-------|---------------|--------------|
| Web | `web` | HttpOnly cookie `amuse_refresh` (omitted from JSON body) | Response body |
| Mobile / default | omit or `mobile` | Response body | Response body |

Cookie `Secure` flag: **`false` in Development**, **`true` otherwise** (see `TokenTransport`).

### Response shape (`AuthTokenResponse`)

```json
{
  "accessToken": "<jwt>",
  "accessExpiresAt": "2026-05-16T12:00:00+00:00",
  "refreshToken": "<opaque or null for web>",
  "refreshExpiresAt": "2026-05-30T12:00:00+00:00"
}
```

Timestamps must be timezone-marked (offset or `Z`).

## Access token (JWT) claims

Minted by `TokenIssuer` in `Amuse.Modules/Identity/Auth/`:

| Claim | When |
|-------|------|
| `sub` | Account ID (GUID) |
| `ctx` | `org` \| `listener` \| `platform` |
| `org_id` | Org persona |
| `listener_id` | Listener persona |
| `org_role` | Preset role label (UI only; org persona) |
| `claims` | Repeated claim entries from read model |
| `jti` | Version 7 GUID (access tokens only) |

Validated on each request: issuer, audience, lifetime, signature (`Jwt` section in config).

## Refresh session

- Opaque refresh token stored hashed in `identity.refresh_session`.
- On refresh: old session **revoked**, new session issued (rotation).
- Refresh row does **not** store persona; client supplies `context` on every refresh.

## Revoke & JTI blacklist

`POST /revoke`:

1. Revokes matching **refresh session** (if refresh token provided via body or cookie).
2. If **`Authorization: Bearer <access>`** is present, parses `jti` + `exp` and inserts **`identity.token_blacklist`** until access expiry.
3. Clears refresh cookie (web).
4. Writes audit entry (`refresh_revoked`).

Subsequent API calls with that access token: JWT middleware checks blacklist → **`401`** with `identity.token_revoked`.

Refresh after revoke → `identity.invalid_refresh_token`.

## Auth flows (concrete)

### 1. Password login (mobile)

```mermaid
sequenceDiagram
    participant Client
    participant API as Identity API
    participant IdP as ASP.NET Identity
    participant RM as Persona read models

    Client->>API: POST /login/password + context
    API->>IdP: Validate email/password
    IdP-->>API: ApplicationUser
    API->>API: Link/load Account
    API->>RM: Resolve persona access
    RM-->>API: PersonaAccessContext
    API->>API: Create refresh_session + JWT
    API-->>Client: accessToken + refreshToken
```

### 2. Password login (web)

Same as above, but client sends `X-Amuse-Client: web` and receives refresh token only in **`Set-Cookie: amuse_refresh`**.

### 3. Refresh / persona switch

```mermaid
sequenceDiagram
    participant Client
    participant API as Identity API
    participant DB as identity schema

    Client->>API: POST /refresh + context + refresh
    API->>DB: Find session by hash, revoke if valid
    API->>API: IssueIdentitySession (new persona)
    API-->>Client: new tokens
```

Use the **same endpoint** when access JWT expires or user picks another org/listener/platform.

### 4. External login

`POST /external/complete` with `provider`, `grantType` (`authorizationCode` \| `idToken`), and grant-specific fields plus `context`.

- Resolves external subject via configured `ExternalProviders` (see `appsettings`).
- `AccountLinker` get-or-create `Account` by `(IdpIssuer, IdpSubject)`.
- Then same session issuance as password login.

### 5. Logout / revoke

`POST /revoke` with refresh (body or cookie) **and** `Authorization: Bearer <current access>` to invalidate access immediately.

### 6. Authenticated reads

- `GET /me` — account id, IdP issuer/subject, status.
- `GET /personas` — aggregates org/listener/platform listings via read models (no cross-BC DbContext access from Identity handlers).

## Domain errors (`IdentityErrors`)

| Code | Typical HTTP | When |
|------|--------------|------|
| `identity.invalid_credentials` | 400 | Bad email/password |
| `identity.account_disabled` | 400 | Account not enabled |
| `identity.invalid_refresh_token` | 400 | Missing/invalid/expired refresh |
| `identity.invalid_persona_context` | 400 | Persona not allowed or malformed |
| `identity.external_login_failed` | 400 | External provider failure |
| `identity.token_revoked` | 401 | Blacklisted `jti` |

Problems use `title` = code, `detail` = message, extension `code` (see `ProblemDetailsMappingExtensions`).

## Configuration

| Section | Purpose |
|---------|---------|
| `ConnectionStrings:DefaultConnection` | Postgres |
| `Jwt` | Issuer, audience, signing key, access/refresh lifetimes |
| `ExternalProviders:Providers` | Named OIDC/OAuth2 provider definitions |
| `Platform:Root` | Seed root platform operator + optional dev `ApplicationUser` |

Dev defaults: [`backend/src/Amuse.Api/appsettings.Development.json`](../../backend/src/Amuse.Api/appsettings.Development.json).

## Code layout (Identity module)

```
Amuse.Modules/Identity/
  Features/<UseCase>/     # Endpoint, Request, Handler, Validator
  Auth/                   # TokenIssuer, AccountLinker, IssueIdentitySession, JWT blacklist
  Auth/External/          # OAuth/OIDC resolvers
  Contracts/              # Cross-BC read-model interfaces + DTOs
  Persistence/            # IdentityDbContext, migrations
  Options/                # JwtOptions, ExternalProviderOptions
  IdentityModule.cs       # DI + MapIdentityModule
```

**Not** a separate application-service layer: orchestration lives in **handlers**; shared minting is `IssueIdentitySession` (internal static helper only).

## Related schemas (other modules)

| Schema | Tables (relevant) |
|--------|-------------------|
| `identity` | `account`, `refresh_session`, `token_blacklist`, ASP.NET Identity tables |
| `tenancy` | `organization_member`, … |
| `listener` | `listener_profile` |
| `platform` | `platform_operator` (root `id = 1`) |
| `audit` | `audit_log` |
