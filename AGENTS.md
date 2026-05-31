Some basic docs which can be used as source of truth is in `ads` folder. Some AI-generated content which can be used as a reference or inspiration can be found in `design` but is not final or authoritative and is NOT source of truth.

Use up-to-date docs from the official sources, or consult from other non-official sources or context7.

Always answer in English. Do not assume anything, ask clarifying questions if needed or not clear from given context.

After an entire feature is completed (and prompted by me, do not automatically do it), document all relevant informations, which includes (but not limited to) what you did, concrete behavior/flows/..., verifying steps, conventions and rules we established, learnings made, etc., into `ai-docs/backend` or `ai-docs/frontend` for which project is being made.

When planning, be as detailed and specific as possible, covering and documenting all possible major painpoints and possible hurdles, and if unsure ask the user, do not assume or gloss over decisions. The plan will be read and implemented by an inexperienced intern who will take shortcuts whenever possible, so guard against that.

When making changes, implementing a new features etc. You MUST ALWAYS consider whether the feature is properly gated by an appropriate claims (either new or existing) and whether improvements will be needed for a soundly designed claim system. More details on claims and auth can be found in the docs.

# .NET C#

Adhere to DDD-style patterns, like rich domain models, only valid state (if a state is invalid, it shouldn't be able to exist/instatiate into a domain model), etc.

Always use DomainErrors reporting and result types, do not use exceptions for expected conditions. Always make domain error in the common domain and use that, not a local string to allow for centralized error messages/code.

Do not treat current codebase as coding standard to follow, only as current behavior and look for possible improvements always.

Have separate config classes/methods for each bounded context.

Move over to vertical slice and DDD-style domain layer, that means prioritize pure domain layer, with value objects (readonly record/record structs) to avoid hidden bugs. Rich domain layer is preferred. Any improvements over old codebase is preferred.

Also, note to use postgres enums, and ALL TIMESTAMPS MUST BE TIMEZONE-MARKED. Require this in the dto validation too, do not allow local timestamps (no Z or +-XX:00)

DO NOT USE `ConfigureAwait(false)`. `SynchronizationContext` has been removed from ASP.NET Core, no need to configure any more.

Every new possible errors added must be using the project's pattern of Problems with error codes and messages registered, and must document that in the endpoint's openapi docs. **Always keep openapi validations generation from componentmodel aligned with validation changes**.

Use Version7 GUID.