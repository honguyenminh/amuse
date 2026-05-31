# Amuse Auth

Amuse is a B2B2C app. That means we have both conventional customers (B2C) and organization (aka business) customers (B2B) which can have many members, each can have their own permissions. This will be a hard challenge, as such we will tackle them one-by-one.

## Account

An `Account` is a separate, standalone entity representing a single `sign-in-able` entity. This only carry basic information like ID linked to the identity provider, metadata like creation date, email, status,...

The actual authentication is done by a separate identity provider, this is to allow multiple sign-in options like social OAuth, passkeys, multi-factor authentication, and of course ordinary username/password. The `Account` entity itself does **NOT** hold any data about these, and will be provided by the identity provider to keep the auth solution abstract and implementation-agnostic.

To actually make `Account` do something useful, we would need to create an actual role-representing entity from this called **persona**. There are 3 types of persona - Organization member, listener, and platform operators; all of which will be described next.

## Organization

Amuse MUST support proper multi-tenant auth. As such, we will:

- Have an overarching `Organization` entity to represent a target business entity in B2B model.
- Separate the concept of `Account` (individual log-in-able entity), `User` (entity representing consumer user - a listener), and `OrganizationMember` (represent a membership of an `Account` within an `Organization`).
- An `Organization` can have many `OrganizationMember`, an `OrganizationMember` only belongs to one `Organization`.
- An `Organization` **MUST ALWAYS have an owner** `OrganizationMember`. The owner `OrganizationMember` must have **at least full admin-level permission**, must **always be a member** of the `Organization`, **cannot be demoted or kicked** by anyone else in the tenant, and have the option to **transfer ownership** to another `OrganizationMember` (above rules still apply).
- If forced owner changes are needed (in case of owner account losing auth credentials, leaving the company, is compromised, legal reasons, etc.), another admin must contact *platform admins* (through support email perhaps) to request such changes.
- *Platform admins* MUST be able to force-change owner `OrganizationMember` to another `OrganizationMember`.

## OrganizationMember / Claim-based access control

First, `OrganizationMember` **IS NOT** an `Account`. An `OrganizationMember` only belongs to one `Organization`, but an `Account` can have many `OrganizationMember`, and as such, `Account` can practically be member of many `Organization`.

We support **claims-based access control**. Each member has a `claims[]` array describing what they can do in the org, instead of relying on a persisted role enum.

### Claim format

Claims use **`{action}:{scope}:{target}`** (three or four colon-separated segments):

- **Scope-wide:** `read:catalog:all`, `manage:membership:all`
- **Per-resource (catalog):** `read:catalog:artist:{guid}` — resource kind and id are separated by `:` so GUIDs may contain hyphens.

The keyword **`all`** means the entire scope. Authorization matches an exact claim or the corresponding `action:scope:all` grant. See [permissions.md](permissions.md) for the catalog and presets.

### Preset roles (UI only)

Preset labels (`admin`, `member_manager`, `catalog_editor`, `viewer`, …) are **UI/frontend convenience**. Selecting a preset **copies** its claim list onto the member or invite row at assign time. Changing preset definitions later does **not** update existing members.

There is NO GUARANTEE that a preset will not change over time.

### Membership management (implemented)

- Email invite with token link (signup or login, then accept).
- List/update/remove members; transfer ownership (owner + `manage:org:all`).
- Org persona JWT must be **refreshed** after claim changes (FR-007).

## Listener

All `Account` entities can have a `Listener` linked to it, which represents a listener profile. This is the main subject of the B2C flow. This is pretty straight forward, and should be automatically created (by the frontend?) when going to the public-facing listener web/app.

## Platform operators

This represent all platform operators for Amuse platform as a whole, this includes Admins, moderators, agents, etc. This also uses the same claims-based model as org member and preset "roles" that are only preset-like and are not persisted too, but this time there is no multiple tenant and there's only a single tenant - ours, which is implicit so should not be represented. This persona is only used on the platform operation portal.

Platform operator account uses a counting-up system instead of UUID like other tables. (Also a fun way to track our company's employees history, imagine having account number 67.)

Only exception to the default claims-based system is the root account, which **only one will exists ever**, with its Platform operators table record's `id` as `1`, call it `player one` or something to keep it cool (lmao), and will have control over all endpoints. This account may be seeded along with migrations. At token mint, root receives `platform:root` in addition to stored claims.
