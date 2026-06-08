/**
 * Shared layout tokens for the consumer app shell and page content.
 *
 * Keep TopBar, AppShell `<main>`, and MiniPlayer horizontal inset aligned.
 * Page bodies use {@link PageContent} for max-width only — do not add outer padding.
 */

/** Horizontal inset shared by TopBar, MiniPlayer controls, and playing-page header. */
export const shellContentPaddingClass = "px-4 md:px-6";

/** Padding on AppShell scrollable `<main>` (and playing-page content when full-screen). No bottom pad — mini player sits flush below. */
export const mainScrollPaddingClass = "px-4 pt-4 md:px-6 md:pt-6";

/** Playing page is full-screen without MiniPlayer — include bottom inset. */
export const playingPageContentPaddingClass =
  "px-4 pt-4 pb-4 md:px-6 md:pt-6 md:pb-6";

export const pageContentBaseClass = "mx-auto flex w-full flex-col";

export type PageContentWidth = "catalog" | "settings" | "account" | "full";

export const pageContentWidthClass: Record<PageContentWidth, string> = {
  catalog: "max-w-7xl",
  settings: "max-w-2xl",
  account: "max-w-xl",
  full: "max-w-none",
};
