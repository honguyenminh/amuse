# Amuse Auth

Amuse is a B2B2C app. That means we have both conventional customers (B2C) and organization (aka business) customers (B2B) which can have many members, each can have their own permissions. This will be a hard challenge, as such we will tackle them one-by-one.

## Account

An `Account` is a separate, standalone entity representing a single `sign-in-able` entity. This only carry basic information like ID linked to the identity provider, metadata like creation date, email, status,...

The actual authentication is done by a separate identity provider, this is to allow multiple sign-in options like social OAuth, passkeys, multi-factor authentication, and of course ordinary username/password. The `Account` entity itself does **NOT** hold any data about these, and will be provided by the identity provider to keep the auth solution abstract and implementation-agnostic.

To actually make `Account` do something useful, we would need to create an actual role-representing entity from this, which will be described next.

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

We support **claims-based access control**. This means, each member will have a claims array describing what they can do in the org, instead of relying on a "role".

A claim is in the form of `resource:action`. Examples include `catalog:read`, `membership:manage`, `analytics:read`, etc. (note that these example claims is not representative of actual claims used in the app, that will be denoted in its own document).

However, to simplify member management, there will be "preset roles" available to select, which will hold a set of claims to quickly assign to members, like "admin", "accountant", etc. These "preset roles" are **UI/FRONTEND ONLY**, and does not mean anything under-the-hood. 

Also, there is NO GUARANTEE that a "preset role" will not change. If later on, the presets are updated, existing members' claims will **NOT get updated**. These are purely presets.

## Auditing

