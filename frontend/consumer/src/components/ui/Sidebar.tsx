"use client";

import { UserAccountChip } from "@/components/account/UserAccountChip";
import { Button } from "@/components/ui/Button";
import { IconButton } from "@/components/ui/IconButton";
import {
  CloseIcon,
  HomeIcon,
  LibraryIcon,
  LogoIcon,
  SearchIcon,
  SettingsIcon,
} from "@/components/ui/NavIcons";
import { Text } from "@/components/ui/Text";
import { useAuth } from "@/lib/auth/AuthProvider";
import { cn } from "@/lib/cn";
import Link from "next/link";
import type { ReactNode } from "react";

type NavLinkProps = {
  href: string;
  icon: ReactNode;
  label: string;
  active: boolean;
  onClick?: () => void;
};

function NavLink({ href, icon, label, active, onClick }: NavLinkProps) {
  return (
    <Link
      href={href}
      onClick={onClick}
      className={cn(
        "flex items-center gap-3 rounded-md px-3 py-2 text-label-large transition-colors",
        active
          ? "bg-primary text-on-primary"
          : "text-on-surface hover:bg-surface-variant",
      )}
    >
      <span
        className={cn(
          "flex h-6 w-6 items-center justify-center",
          active ? "text-on-primary" : "text-on-surface-variant",
        )}
      >
        {icon}
      </span>
      <span>{label}</span>
    </Link>
  );
}

type SidebarProps = {
  activePath: string;
  /** `drawer` = full-width panel inside the mobile slide-over; `rail` = desktop left rail. */
  variant?: "rail" | "drawer";
  onNavigate?: () => void;
  onClose?: () => void;
};

export function Sidebar({
  activePath,
  variant = "rail",
  onNavigate,
  onClose,
}: SidebarProps) {
  const auth = useAuth();
  const isDrawer = variant === "drawer";
  const isActive = (prefix: string) =>
    activePath === prefix || activePath.startsWith(`${prefix}/`);

  return (
    <aside
      className={cn(
        "flex flex-col gap-4 bg-surface px-3 py-4",
        isDrawer
          ? "h-full min-h-0 w-full overflow-y-auto"
          : "h-full w-64 shrink-0 border-r-2 border-outline",
      )}
    >
      <div
        className={cn(
          "flex items-center gap-2",
          isDrawer ? "justify-between px-1" : "px-3 py-1",
        )}
      >
        <Link
          href="/home"
          onClick={onNavigate}
          className="flex min-w-0 items-center gap-2"
        >
          <LogoIcon className="shrink-0 text-primary" />
          <Text variant="title-large" className="truncate tracking-tight">
            Amuse
          </Text>
        </Link>
        {isDrawer && onClose ? (
          <IconButton label="Close menu" variant="ghost" size="md" onClick={onClose}>
            <CloseIcon />
          </IconButton>
        ) : null}
      </div>

      <nav className="flex flex-col gap-1">
        <NavLink
          href="/home"
          icon={<HomeIcon />}
          label="Home"
          active={isActive("/home")}
          onClick={onNavigate}
        />
        <NavLink
          href="/search"
          icon={<SearchIcon />}
          label="Search"
          active={isActive("/search")}
          onClick={onNavigate}
        />
        <NavLink
          href="/library"
          icon={<LibraryIcon />}
          label="Library"
          active={isActive("/library")}
          onClick={onNavigate}
        />
        <NavLink
          href="/settings"
          icon={<SettingsIcon />}
          label="Settings"
          active={isActive("/settings")}
          onClick={onNavigate}
        />
      </nav>

      <div className="mt-auto flex flex-col gap-2 border-t-2 border-outline pt-3">
        {auth.isAuthenticated ? (
          <UserAccountChip />
        ) : (
          <>
            <Text variant="label-medium" className="px-3 text-on-surface-variant">
              Browsing as a guest
            </Text>
            <Link href="/login" onClick={onNavigate}>
              <Button type="button" className="w-full">
                Log in
              </Button>
            </Link>
          </>
        )}
      </div>
    </aside>
  );
}
