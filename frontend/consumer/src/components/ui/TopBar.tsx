"use client";

import { IconButton } from "@/components/ui/IconButton";
import { MenuIcon, SearchIcon, UserIcon } from "@/components/ui/NavIcons";
import { Text } from "@/components/ui/Text";
import { useAuth } from "@/lib/auth/AuthProvider";
import { cn } from "@/lib/cn";
import Link from "next/link";

type TopBarProps = {
  title: string;
  trailing?: React.ReactNode;
  onMenuClick: () => void;
};

/**
 * Top chrome of the app shell. On `md+` it shows a search affordance and the
 * account/login control. On mobile it surfaces a hamburger button (opens the
 * MobileDrawer with the same nav) plus the route title; search collapses into
 * an icon button. `trailing` is reserved for page-specific actions.
 */
export function TopBar({ title, trailing, onMenuClick }: TopBarProps) {
  const auth = useAuth();

  return (
    <header
      className={cn(
        "sticky top-0 z-30 flex items-center gap-3 border-b-2 border-outline bg-surface/90 px-3 py-2 backdrop-blur",
      )}
    >
      <IconButton
        label="Open menu"
        variant="ghost"
        size="md"
        onClick={onMenuClick}
        className="md:hidden"
      >
        <MenuIcon />
      </IconButton>

      <Text as="h1" variant="title-large" className="min-w-0 flex-1 truncate">
        {title}
      </Text>

      {/* Desktop search field — placeholder; routes to /search once that exists. */}
      <Link
        href="/search"
        className={cn(
          "hidden items-center gap-2 rounded-full border-2 border-outline bg-background px-3 py-1.5 text-on-surface-variant hover:bg-surface-variant md:flex",
        )}
        aria-label="Search"
      >
        <SearchIcon />
        <span className="text-label-medium">Search artists, releases, tracks</span>
      </Link>

      <IconButton
        label="Search"
        variant="ghost"
        size="md"
        onClick={() => {
          /* Mobile entry point to the same search route. */
          window.location.assign("/search");
        }}
        className="md:hidden"
      >
        <SearchIcon />
      </IconButton>

      {trailing}

      {auth.isAuthenticated ? (
        <IconButton
          label="Account"
          variant="ghost"
          size="md"
          onClick={() => void auth.logout()}
        >
          <UserIcon />
        </IconButton>
      ) : (
        <Link
          href="/login"
          className="inline-flex shrink-0 items-center gap-2 rounded-full border-2 border-outline bg-primary px-3 py-1.5 text-label-medium text-on-primary hover:opacity-90"
        >
          Log in
        </Link>
      )}
    </header>
  );
}
