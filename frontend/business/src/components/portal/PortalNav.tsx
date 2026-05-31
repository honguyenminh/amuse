"use client";

import {
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/components/ui/sidebar";
import { useAuth } from "@/lib/auth/AuthProvider";
import { isPlatformPersonaActive } from "@/lib/auth/resolveBusinessPersonas";
import { hasClaim } from "@/lib/auth/jwtClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import { ClipboardList, LayoutDashboard, Settings, Users } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useMemo } from "react";

const orgNavBase = [
  { href: "/dashboard", label: "Dashboard", icon: LayoutDashboard },
  { href: "/members", label: "Members", icon: Users },
  { href: "/settings", label: "Settings", icon: Settings },
] as const;

const platformNavItems = [
  {
    href: "/platform/applications",
    label: "Applications",
    icon: ClipboardList,
  },
  { href: "/settings", label: "Settings", icon: Settings },
] as const;

export function PortalNav() {
  const pathname = usePathname();
  const auth = useAuth();
  const isPlatform = isPlatformPersonaActive(auth.activePersona);
  const canReadMembers = hasClaim(getAccessToken(), "read:membership:all");
  const orgNavItems = useMemo(
    () =>
      orgNavBase.filter(
        (item) => item.href !== "/members" || canReadMembers,
      ),
    [canReadMembers],
  );
  const navItems = isPlatform ? platformNavItems : orgNavItems;
  const groupLabel = isPlatform ? "Platform" : "Organization";

  return (
    <SidebarGroup>
      <SidebarGroupLabel>{groupLabel}</SidebarGroupLabel>
      <SidebarGroupContent>
        <SidebarMenu>
          {navItems.map(({ href, label, icon: Icon }) => (
            <SidebarMenuItem key={href}>
              <SidebarMenuButton
                render={<Link href={href} />}
                isActive={pathname === href || pathname.startsWith(`${href}/`)}
                tooltip={label}
              >
                <Icon />
                <span>{label}</span>
              </SidebarMenuButton>
            </SidebarMenuItem>
          ))}
        </SidebarMenu>
      </SidebarGroupContent>
    </SidebarGroup>
  );
}
