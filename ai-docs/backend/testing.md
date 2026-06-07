# Testing

## Unit tests


| Project                                                                             | Scope                                                                                                  |
| ----------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| `[Amuse.Domain.Tests](../../backend/tests/Amuse.Domain.Tests/)`                     | Domain VOs, aggregates, `PersonaContext`, etc.                                                         |
| `[Amuse.Modules.Identity.Tests](../../backend/tests/Amuse.Modules.Identity.Tests/)` | `TokenIssuer`, `IssueIdentitySession`, `RevokeTokenHandler` (blacklist), in-memory `IdentityDbContext` |


Run:

```bash
cd backend
dotnet test tests/Amuse.Modules.Identity.Tests
dotnet test tests/Amuse.Domain.Tests
```

`Amuse.Modules` exposes `InternalsVisibleTo` `Amuse.Modules.Identity.Tests` for internal auth helpers.

## Integration tests

Project: `[Amuse.Api.IntegrationTests](../../backend/tests/Amuse.Api.IntegrationTests/)`

### Requirements

- **Docker** running (Testcontainers starts `postgres:16` and `redis:8.8-alpine` per collection fixture).
- First run downloads the Postgres and Redis images.

### Fixture

- `AmuseApiFixture` — `WebApplicationFactory<Program>`, overrides connection string, runs `ModuleDatabaseInitializer.MigrateAllAsync`.
- Config: `appsettings.Testing.json` (mirrors dev JWT + Platform root seed).
- Collection: `[Collection(AmuseApiCollection.Name)]`.

`Program` is exposed via `[Program.Integration.cs](../../backend/src/Amuse.Api/Program.Integration.cs)` (`public partial class Program`).

### Scenarios covered (`IdentityAuthFlowTests`)


| Test                                               | Asserts                                         |
| -------------------------------------------------- | ----------------------------------------------- |
| `LoginPassword_mobile_returns_tokens`              | Access + refresh in body                        |
| `GetCurrentAccount_with_access_token_succeeds`     | `GET /me` 200                                   |
| `ListPersonas_includes_platform`                   | Platform persona listed                         |
| `Refresh_issues_new_access_token`                  | New access JWT after refresh                    |
| `Revoke_blacklists_access_and_invalidates_refresh` | 401 `identity.token_revoked`; refresh fails     |
| `LoginPassword_web_uses_refresh_cookie`            | Cookie-based refresh without body refresh token |


Run:

```bash
cd backend
dotnet test tests/Amuse.Api.IntegrationTests
```

If Docker is unavailable, tests fail with `DockerUnavailableException` — expected in environments without a Docker socket.

## CI recommendation

- Job with Docker-in-Docker (or sibling Docker socket).
- `dotnet test backend.slnx` on the solution (includes integration + `Amuse.Modules.Identity.Tests`).
- Do **not** rely on startup migrations in the test host — fixture migrates explicitly.

## Manual vs automated parity

Integration tests exercise the same HTTP contract documented in [local-development.md](./local-development.md). After changing auth behavior, update both tests and `ai-docs` if contracts shift.

### Catalog + playback contract (`CatalogEndpointsTests`)


| Test                                                        | Asserts                                                                                                                                                           |
| ----------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Stream_info_returns_track_stream_not_ready_until_ingested` | Authenticated `GET .../stream-info` returns **400** `catalog.track_stream_not_ready` when no worker has set `audio_stream_key` (DASH-only; no master signed URL). |
| `Stream_info_requires_authentication`                       | Anonymous `stream-info` → **401**                                                                                                                                 |
| `Stream_info_unknown_track_returns_problem`                 | Missing track → problem response                                                                                                                                  |


Browse endpoints (`home`, artist, release) remain anonymous-friendly.