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
import { hasAnyCatalogReadClaim, hasClaim } from "@/lib/auth/jwtClaims";
import {
  canManagePlatformPayouts,
  canManagePlatformPurchases,
  canReadPlatformAccounting,
} from "@/lib/auth/platformClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import {
  ClipboardList,
  Disc3,
  LayoutDashboard,
  Receipt,
  Settings,
  ShoppingCart,
  Users,
  Wallet,
} from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useMemo } from "react";

type NavItem = {
  href: string;
  label: string;
  icon: typeof LayoutDashboard;
};

const orgNavBase: NavItem[] = [
  { href: "/dashboard", label: "Dashboard", icon: LayoutDashboard },
  { href: "/members", label: "Members", icon: Users },
  { href: "/settings", label: "Settings", icon: Settings },
];

const platformNavBase: NavItem[] = [
  { href: "/platform/applications", label: "Applications", icon: ClipboardList },
  { href: "/settings", label: "Settings", icon: Settings },
];

function buildPlatformNavItems(token: string | null): NavItem[] {
  const items = [...platformNavBase];
  const financeItems: NavItem[] = [];

  if (canReadPlatformAccounting(token)) {
    financeItems.push({
      href: "/platform/accounting",
      label: "Accounting",
      icon: Receipt,
    });
  }

  if (canManagePlatformPurchases(token)) {
    financeItems.push({
      href: "/platform/purchases",
      label: "Purchases",
      icon: ShoppingCart,
    });
  }

  if (canManagePlatformPayouts(token)) {
    financeItems.push(
      {
        href: "/platform/payout-profiles",
        label: "Payout review",
        icon: Wallet,
      },
      {
        href: "/platform/withdrawals",
        label: "Withdrawals",
        icon: Wallet,
      },
    );
  }

  if (financeItems.length > 0) {
    items.splice(1, 0, ...financeItems);
  }

  return items;
}

export function PortalNav() {
  const pathname = usePathname();
  const auth = useAuth();
  const isPlatform = isPlatformPersonaActive(auth.activePersona);
  const token = getAccessToken();
  const canReadMembers = hasClaim(token, "read:membership:all");
  const canReadCatalog = hasAnyCatalogReadClaim(token);
  const canReadPayout = hasClaim(token, "read:payout:all");
  const orgNavItems = useMemo(() => {
    const items = [...orgNavBase];
    if (canReadCatalog) {
      items.splice(1, 0, { href: "/catalog", label: "Catalog", icon: Disc3 });
    }
    if (canReadPayout) {
      items.splice(items.length - 1, 0, {
        href: "/finance/balance",
        label: "Finance",
        icon: Wallet,
      });
    }
    return items.filter((item) => item.href !== "/members" || canReadMembers);
  }, [canReadMembers, canReadCatalog, canReadPayout]);
  const navItems = isPlatform ? buildPlatformNavItems(token) : orgNavItems;
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
