Some basic docs which can be used as source of truth is in `ads` folder. Some AI-generated content which can be used as a reference or inspiration can be found in `design` but is not final or authoritative and is NOT source of truth.

Use up-to-date docs from the official sources, or consult from other non-official sources or context7.

Always answer in English. Do not assume anything, ask clarifying questions if needed or not clear from given context.

After an entire feature is completed (and prompted by me, do not automatically do it), document all relevant informations, which includes (but not limited to) what you did, concrete behavior/flows/..., verifying steps, conventions and rules we established, learnings made, etc., into `ai-docs/backend` or `ai-docs/frontend` for which project is being made.

When planning, be as detailed and specific as possible, covering and documenting all possible major painpoints and possible hurdles, and if unsure ask the user, do not assume or gloss over decisions. The plan will be read and implemented by an inexperienced intern who will take shortcuts whenever possible, so guard against that.

Plan extremely carefully. Give very detailed implementations steps with proper code changes plan, not just a vague bulletpoint list. Any ambiguity and unclear points should be asked, and asked as much as possible, do not assume anything or rely on training data.

When making changes, implementing a new features etc. You MUST ALWAYS consider whether the feature is properly gated by an appropriate claims (either new or existing) and whether improvements will be needed for a soundly designed claim system. More details on claims and auth can be found in the docs.

**Platform operator claims:** Never gate features with a raw JWT check for only `manage:platform:organizations`. The break-glass `platform:root` operator must receive full platform access via `PlatformClaims` (`Amuse.Domain/Platform/PlatformClaims.cs`) on the backend and `frontend/business/src/lib/auth/platformClaims.ts` on the business portal. Token mint expands root to explicit manage/review claims, but handlers and UI must still use the shared helpers so implied grants are not missed.

Must use pnpm for frontend actions.

# .NET C#

Adhere to VSA but follow DDD-style patterns, like rich domain models, only valid state (if a state is invalid, it shouldn't be able to exist/instatiate into a domain model), etc. Note this VERY EXPLICITLY in the plans, etc. As LLM models have a tendency to ignore this guidance and do very crude VSA where it calls to dbset in the api handler directly, bypassing the domain layer.

Always use DomainErrors reporting and result types, do not use exceptions for expected conditions. Always make domain error in the common domain and use that, not a local string to allow for centralized error messages/code.

Do not treat current codebase as coding standard to follow, only as current behavior and look for possible improvements always.

Have separate config classes/methods for each bounded context.

Move over to vertical slice and DDD-style domain layer, that means prioritize pure domain layer, with value objects (readonly record/record structs) to avoid hidden bugs. Rich domain layer is preferred. Any improvements over old codebase is preferred.

Also, note to use postgres enums, and ALL TIMESTAMPS MUST BE TIMEZONE-MARKED. Require this in the dto validation too, do not allow local timestamps (no Z or +-XX:00)

DO NOT USE `ConfigureAwait(false)`. `SynchronizationContext` has been removed from ASP.NET Core, no need to configure any more.

Every new possible errors added must be using the project's pattern of Problems with error codes and messages registered, and must document that in the endpoint's openapi docs. **Always keep openapi validations generation from componentmodel aligned with validation changes**.

Use Version7 GUID.

# Docker

All Dockerfiles use [BuildKit cache mounts](https://docs.docker.com/build/cache/optimize/#use-cache-mounts) on package-install `RUN` steps to speed up local and CI rebuilds:

- **NuGet** — mount `/root/.nuget/packages` and `/root/.local/share/NuGet/http-cache` on every `dotnet restore`, `dotnet build`, and `dotnet publish` in multi-stage images
- **apt** — mount `/var/cache/apt` and `/var/lib/apt` on `apt-get install` (transcoder worker)

Use `sharing=locked` on cache mounts. Keep the `# syntax=docker/dockerfile:1` directive at the top of each Dockerfile.

BuildKit must be enabled (`DOCKER_BUILDKIT=1`; default in Docker 23+ and `docker compose build`). When adding a new Dockerfile or a `RUN` step that downloads packages, apply the same cache mounts.

**Exception:** do not use NuGet cache mounts when the restored packages or tools must remain in the final image (e.g. `Dockerfile.migrate` runs `dotnet ef` at container start, so `dotnet tool restore` must bake packages into the image layer).