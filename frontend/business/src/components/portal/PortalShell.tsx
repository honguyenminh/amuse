"use client";

import { OrganizationStatusBanner } from "@/components/portal/OrganizationStatusBanner";
import { PersonaSwitcher } from "@/components/portal/PersonaSwitcher";
import { PortalNav } from "@/components/portal/PortalNav";
import { PortalUserChip } from "@/components/account/PortalUserChip";
import { Button } from "@/components/ui/button";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarHeader,
  SidebarInset,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarTrigger,
} from "@/components/ui/sidebar";
import { useAuth } from "@/lib/auth/AuthProvider";
import { isPlatformPersonaActive } from "@/lib/auth/resolveBusinessPersonas";
import { LogOut } from "lucide-react";
import { usePathname, useRouter } from "next/navigation";
import type { ReactNode } from "react";

const pageTitles: Record<string, string> = {
  "/dashboard": "Dashboard",
  "/members": "Members",
  "/settings": "Settings",
  "/settings/account": "Account",
  "/platform/applications": "Applications",
  "/members/invites": "Pending invites",
};

type PortalShellProps = {
  children: ReactNode;
};

export function PortalShell({ children }: PortalShellProps) {
  const auth = useAuth();
  const pathname = usePathname();
  const router = useRouter();
  const isPlatform = isPlatformPersonaActive(auth.activePersona);
  const title =
    pageTitles[pathname]
    ?? (pathname.startsWith("/members") ? "Members" : null)
    ?? (pathname.startsWith("/settings") ? "Settings" : null)
    ?? (isPlatform ? "Platform console" : "Console");

  async function onSignOut() {
    await auth.logout();
    router.replace("/login");
  }

  return (
    <SidebarProvider>
      <Sidebar collapsible="icon" variant="sidebar">
        <SidebarHeader>
          <SidebarMenu>
            <SidebarMenuItem>
              <SidebarMenuButton size="lg" className="pointer-events-none">
                <div className="flex aspect-square size-8 items-center justify-center rounded-lg bg-primary text-primary-foreground">
                  <span className="text-sm font-semibold">A</span>
                </div>
                <div className="grid flex-1 text-left text-sm leading-tight">
                  <span className="truncate font-semibold">Amuse</span>
                  <span className="truncate text-xs text-muted-foreground">
                    {isPlatform ? "Platform" : "Organization"}
                  </span>
                </div>
              </SidebarMenuButton>
            </SidebarMenuItem>
          </SidebarMenu>
        </SidebarHeader>
        <SidebarContent>
          <PortalNav />
        </SidebarContent>
        <SidebarFooter>
          <SidebarMenu>
            <SidebarMenuItem>
              <SidebarMenuButton size="lg" render={<PortalUserChip className="w-full" />} />
            </SidebarMenuItem>
          </SidebarMenu>
        </SidebarFooter>
      </Sidebar>
      <SidebarInset className="min-h-svh">
        <header className="sticky top-0 z-30 flex h-14 shrink-0 items-center gap-3 border-b bg-background/95 px-4 backdrop-blur supports-backdrop-filter:bg-background/80">
          <SidebarTrigger className="-ml-1" />
          <h1 className="flex-1 text-sm font-medium">{title}</h1>
          <div className="flex items-center gap-2">
            <PersonaSwitcher compact />
            <Button variant="ghost" size="sm" onClick={() => void onSignOut()}>
              <LogOut />
              Sign out
            </Button>
          </div>
        </header>
        <div className="flex flex-1 flex-col gap-4 bg-muted/40 p-4 md:p-6 pb-16">
          <OrganizationStatusBanner />
          {children}
        </div>
      </SidebarInset>
    </SidebarProvider>
  );
}
