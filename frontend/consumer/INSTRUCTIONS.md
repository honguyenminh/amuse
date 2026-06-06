# Consumer frontend — layout conventions

This document defines how page margins, shell chrome, and content width work in the consumer app. Follow these rules when adding or changing routes so spacing stays consistent.

## Standard page inset

All **AppShell** routes share one outer inset:

| Token | Tailwind | Used on |
| --- | --- | --- |
| `shellContentPaddingClass` | `px-4 md:px-6` | TopBar, MiniPlayer control row, `/playing` header |
| `mainScrollPaddingClass` | `p-4 md:p-6` | AppShell `<main>`, `/playing` scroll body |

Source of truth: `src/lib/ui/pageLayout.ts`.

- **16px** horizontal padding on small viewports (`p-4` / `px-4`)
- **24px** from `md` and up (`md:p-6` / `md:px-6`)
- TopBar title and page body left edge must stay aligned — do not pick ad-hoc values like `p-4` only or `md:px-12` on shell pages.

## Page content width

Inside AppShell `<main>`, use **`PageContent`** (`src/components/ui/PageContent.tsx`) for max-width and vertical stacking only. **Do not add outer padding** on page wrappers; AppShell already applies `mainScrollPaddingClass`.

```tsx
<AppShell title="Home" activePath="/home">
  <PageContent gap="8">{/* sections */}</PageContent>
</AppShell>
```

| `width` prop | Max width | Typical routes |
| --- | --- | --- |
| `catalog` (default) | `max-w-7xl` | Home, artist, release |
| `settings` | `max-w-2xl` | Playback settings |
| `account` | `max-w-xl` | Account settings |
| `full` | none | Rare full-bleed layouts inside shell |

Use the `gap` prop (`4`, `6`, or `8`) for section spacing instead of inventing new page-level gaps.

## Route patterns

### AppShell pages (most of the app)

Home, artist, release, settings, account settings, etc.:

1. Wrap in `AppShell` with `title` + `activePath`.
2. Wrap body in `PageContent` with the appropriate `width` / `gap`.
3. Section spacing inside cards/sections may use `gap-3`, `gap-4`, etc. — that is internal layout, not page margin.

### Full-screen routes (exceptions)

These **do not** use AppShell and define their own centered or edge-to-edge layout:

| Route | Layout |
| --- | --- |
| `/playing` | Full viewport player; uses `shellContentPaddingClass` + `mainScrollPaddingClass` from `pageLayout.ts` |
| `/onboarding` | Centered card, `p-6` on the outer column |
| `/login`, `/signup`, `/confirm-email` | Centered auth forms, `p-6` |
| B2cGate / layout loading fallbacks | Centered, `p-8` |

When adding a new full-screen route, reuse the shared padding tokens where horizontal alignment with the rest of the app matters (see `/playing`).

## Popups and overlays

Use **`AnchoredPopup`** (`src/components/ui/AnchoredPopup.tsx`) for anchor-based menus (account chip, quality picker, context menus). It portals to `document.body` and avoids viewport overflow via `computeAnchoredPosition`.

Do not hand-roll `absolute top-full` menus on shell pages unless there is a strong reason; they clip against scroll containers and misalign near viewport edges.

## Checklist for new pages

- [ ] Uses `AppShell` unless the route is intentionally full-screen.
- [ ] Body uses `PageContent` — no `p-4`, `md:p-6`, or `max-w-*` duplicated on the page wrapper.
- [ ] TopBar title matches route intent; avoid duplicating the same headline in content unless you need a subtitle block (artist/release pages are fine).
- [ ] Menus/popovers use `AnchoredPopup`.
- [ ] If you need a new max-width tier, add it to `pageContentWidthClass` in `pageLayout.ts` and document it here — do not inline a one-off `max-w-*` on a page.

## Anti-patterns (will drift)

```tsx
// Bad — double padding and inconsistent md inset
<div className="mx-auto max-w-7xl p-4 md:p-6">...</div>

// Bad — release-style p-4 only
<div className="flex flex-col gap-4 p-4">...</div>

// Bad — playing-style extra horizontal padding
<div className="p-4 md:px-12">...</div>
```

```tsx
// Good
<AppShell title="Release" activePath="/release">
  <PageContent gap="4">...</PageContent>
</AppShell>
```
