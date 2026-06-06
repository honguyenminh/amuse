"use client";

import { Text } from "@/components/ui/Text";
import {
  libraryAlbumsPath,
  libraryLikedPath,
  libraryPlaylistsPath,
} from "@/lib/discovery/paths";
import { cn } from "@/lib/cn";
import Link from "next/link";
import { usePathname } from "next/navigation";

const tabs = [
  { href: libraryPlaylistsPath, label: "Playlists" },
  { href: libraryLikedPath, label: "Liked" },
  { href: libraryAlbumsPath, label: "Albums" },
] as const;

export function LibraryTabs() {
  const pathname = usePathname();

  return (
    <nav
      className="flex gap-1 border-b-2 border-outline"
      aria-label="Library sections"
    >
      {tabs.map((tab) => {
        const active = pathname === tab.href || pathname.startsWith(`${tab.href}/`);
        return (
          <Link
            key={tab.href}
            href={tab.href}
            className={cn(
              "border-b-2 px-4 py-2 transition-colors",
              active
                ? "border-primary text-primary"
                : "border-transparent text-on-surface-variant hover:text-on-surface",
            )}
          >
            <Text variant="label-large">{tab.label}</Text>
          </Link>
        );
      })}
    </nav>
  );
}
