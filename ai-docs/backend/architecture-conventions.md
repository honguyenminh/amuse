# Backend architecture conventions (as implemented)

Supplements [`ads/backend-structure.md`](../../ads/backend-structure.md) and [`AGENTS.md`](../../AGENTS.md) with **rules established during Identity implementation**.

## Modular monolith

- Single process: **`Amuse.Api`** composes modules.
- One **Postgres database**, one **EF `DbContext` per bounded context**, each with its own **schema** and **migration history table**.
- Cross-BC consistency: events/outbox (future); **never** query another BC’s tables from a handler.

## Vertical slice architecture (VSA)

Each use case under `Amuse.Modules/<BC>/Features/<UseCase>/`:

| File | Role |
|------|------|
| `*Endpoint.cs` | Minimal API route; maps `Result<T>` to `IResult` |
| `*Request.cs` / `*Response.cs` | HTTP DTOs (records) |
| `*Handler.cs` | **Orchestration** — one public `HandleAsync`; plain DI (no MediatR) |
| `*Validator.cs` | FluentValidation (optional) |

**Do not** add a parallel “application service” layer that handlers only delegate to. Shared non–use-case code belongs in:

- **`Auth/`** — technical helpers (JWT, cookies, external IdP, `IssueIdentitySession`)
- **`Contracts/`** — cross-BC read-model **interfaces** consumed by Identity
- **`Persistence/`** — EF

Handlers call read-model interfaces; implementations live in the owning BC (e.g. `TenancyPersonaReadModel`).

## Domain-driven design

- Rich models and value objects in **`Amuse.Domain`** (`AccountId`, `PersonaContext`, etc.).
- **Invalid states should not construct** — VOs may throw on impossible values (empty GUID).
- **Expected failures** use **`Result<T>`** + **`DomainError`** (`IdentityErrors`, etc.) — not exceptions for control flow in handlers/API.
- Use **Version 7 GUIDs** for new IDs where applicable.

## Cross-BC read models (Identity)

Interfaces in `Amuse.Modules/Identity/Contracts/`:

- `ITenancyPersonaReadModel` — org context + `ListAvailableOrgsAsync`
- `IListenerPersonaReadModel` — listener context + profile lookup
- `IPlatformPersonaReadModel` — platform context + `IsPlatformOperatorAsync`
- `IOrganizationCreatorContactLookup` (Tenancy) — creator email/IdP snapshot for backing-org application review; **implemented** in Identity as a persistence adapter only (B2B tenancy concern, not auth domain)

Registered in each BC’s `*Module.cs` (consumer defines port, provider implements). Identity **must not** inject `TenancyDbContext` / `ListenerDbContext` / `PlatformDbContext` in feature handlers.

## Validation & API errors

1. **JSON enums** — camelCase via `JsonStringEnumConverter` in `Program.cs`.
2. **FluentValidation** — `AddValidatorsFromAssemblyContaining<...>()` in `IdentityModule`.
3. **Endpoint filter** — `RequestValidationFilter` via `.WithRequestValidation()` on routes with bodies.
4. **Business errors** — `ProblemDetailsMappingExtensions.ToResult` → 400 with `extensions.code`.
5. **Auth revocation** — JWT pipeline → 401 `identity.token_revoked` for blacklisted `jti`.

New endpoints: document error codes in OpenAPI (`.ProducesProblem`, `.ProducesValidationProblem`).

## Timestamps

All API timestamps must be **timezone-marked** (`DateTimeOffset` with `Z` or `±hh:mm`). Validators should reject naive/local times where DTOs carry instants.

## Postgres enums

Prefer Postgres enums for persisted enums when adding schema (project rule in AGENTS.md); Identity currently maps enums in EF where already migrated.

## Migrations

| Rule | Detail |
|------|--------|
| **No startup migrate** | `Program.cs` does not call `Database.Migrate` |
| **Dev** | `backend/scripts/migrate-all.sh` |
| **Tests** | `ModuleDatabaseInitializer.MigrateAllAsync` in integration fixture only |
| **Per-BC history** | `__EFMigrationsHistory_<bc>` in each schema |

## Module registration pattern

Each BC exposes:

```csharp
public static IServiceCollection AddXxxModule(this IServiceCollection services, IConfiguration configuration);
public static IEndpointRouteBuilder MapXxxModule(this IEndpointRouteBuilder endpoints);
```

`Program.cs` is the only composition root for modules.

## Authorization middleware

- **JWT Bearer** — default scheme; blacklist check on validated tokens.
- **`TenantGuardMiddleware`** — routes marked with `[RequireOrgTenant]` require `ctx=org` and `org_id` claim.

## Audit

Sensitive identity actions (e.g. revoke) write to `audit.audit_log` via `IAuditWriter`.

## What we explicitly avoided

- **MediatR / CQRS buses** for application requests.
- **Stringly-typed persona** `type` fields — use `PersonaContextType` enum in DTOs.
- **Exception-based request parsing** (removed `PersonaContextParser` throwing `ArgumentException`).
- **N-tier “Services” folder** as a second orchestration layer — collapsed into feature handlers + `Auth/` helpers.
- **Storing persona on refresh_session** — persona is client-supplied per refresh.
- **Auto-migrate on API boot in production** (or any environment).

## Configuration per BC

Each module has its own `Options/` types and `Configure<T>(configuration.GetSection(...))` — do not share one mega-options class across BCs.

## Testing conventions

- **Unit:** `Amuse.Domain.Tests`, `Amuse.Modules.Identity.Tests` (in-memory EF where needed).
- **Integration:** `Amuse.Api.IntegrationTests` — real Postgres via Testcontainers; requires Docker.
- Do not use `ConfigureAwait(false)` in ASP.NET Core code paths.
