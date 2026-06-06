/**
 * Shared layout tokens for the consumer app shell and page content.
 *
 * Keep TopBar, AppShell `<main>`, and MiniPlayer horizontal inset aligned.
 * Page bodies use {@link PageContent} for max-width only — do not add outer padding.
 */

/** Horizontal inset shared by TopBar, MiniPlayer controls, and playing-page header. */
export const shellContentPaddingClass = "px-4 md:px-6";

/** Padding on AppShell scrollable `<main>` (and playing-page content when full-screen). */
export const mainScrollPaddingClass = "p-4 md:p-6";

export const pageContentBaseClass = "mx-auto flex w-full flex-col";

export type PageContentWidth = "catalog" | "settings" | "account" | "full";

export const pageContentWidthClass: Record<PageContentWidth, string> = {
  catalog: "max-w-7xl",
  settings: "max-w-2xl",
  account: "max-w-xl",
  full: "max-w-none",
};
