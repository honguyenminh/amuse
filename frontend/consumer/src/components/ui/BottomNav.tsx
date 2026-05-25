import Link from "next/link";
import { cn } from "@/lib/cn";

const links = [
  { href: "/home", label: "Home" },
  { href: "/artist/demo", label: "Artist" },
  { href: "/album/demo", label: "Album" },
] as const;

type BottomNavProps = {
  activePath: string;
};

export function BottomNav({ activePath }: BottomNavProps) {
  return (
    <nav className="flex border-t-2 border-outline bg-surface">
      {links.map((link) => {
        const active = activePath.startsWith(link.href);
        return (
          <Link
            key={link.href}
            href={link.href}
            className={cn(
              "flex-1 py-3 text-center text-label-medium transition-colors",
              active
                ? "bg-primary-container text-on-primary-container"
                : "text-on-surface-variant hover:bg-surface-variant",
            )}
          >
            {link.label}
          </Link>
        );
      })}
    </nav>
  );
}
