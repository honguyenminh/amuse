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
import { ClipboardList, LayoutDashboard, Settings } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";

const orgNavItems = [
  { href: "/dashboard", label: "Dashboard", icon: LayoutDashboard },
  { href: "/settings", label: "Settings", icon: Settings },
] as const;

const platformNavItems = [
  {
    href: "/platform/applications",
    label: "Applications",
    icon: ClipboardList,
  },
] as const;

export function PortalNav() {
  const pathname = usePathname();
  const auth = useAuth();
  const isPlatform = isPlatformPersonaActive(auth.activePersona);
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
