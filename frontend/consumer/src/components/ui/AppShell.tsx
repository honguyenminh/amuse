"use client";

import { MiniPlayer } from "@/components/player/MiniPlayer";
import { MobileDrawer } from "@/components/ui/MobileDrawer";
import { Sidebar } from "@/components/ui/Sidebar";
import { TopBar } from "@/components/ui/TopBar";
import { cn } from "@/lib/cn";
import { mainScrollPaddingClass } from "@/lib/ui/pageLayout";
import { usePathname } from "next/navigation";
import { useEffect, useRef, useState, type ReactNode } from "react";

type AppShellProps = {
  title?: string;
  activePath: string;
  trailing?: React.ReactNode;
  children: ReactNode;
  /** Hide the mini player on routes that own playback chrome themselves (e.g. /playing). */
  hidePlayer?: boolean;
};

/**
 * Application chrome for the consumer app.
 *
 * Layout, locked to `h-dvh` so mobile address-bar shrink doesn't leak the body
 * out from under the player:
 *
 * ```
 * ┌─────────┬────────────────────────┐
 * │         │ TopBar (sticky)        │
 * │ Sidebar │ scrollable main        │
 * │ (md+)   │                        │
 * └─────────┴────────────────────────┘
 * │ MiniPlayer (full-width)           │
 * └───────────────────────────────────┘
 * ```
 *
 * On viewports below `md` the sidebar collapses into a slide-in MobileDrawer
 * toggled by the TopBar's hamburger. The MiniPlayer spans the full viewport
 * width on all sizes; it self-hides when the queue is empty.
 */
export function AppShell({
  title,
  activePath,
  trailing,
  children,
  hidePlayer = false,
}: AppShellProps) {
  const [drawerOpen, setDrawerOpen] = useState(false);
  const mainRef = useRef<HTMLElement>(null);
  const pathname = usePathname();

  useEffect(() => {
    mainRef.current?.scrollTo({ top: 0, left: 0 });
  }, [pathname]);

  return (
    <div className="flex h-dvh flex-col bg-background text-on-background">
      <div className="flex min-h-0 flex-1 overflow-hidden">
        <div className="hidden md:flex">
          <Sidebar activePath={activePath} />
        </div>
        <div className="flex min-w-0 flex-1 flex-col">
          <TopBar
            title={title}
            trailing={trailing}
            onMenuClick={() => setDrawerOpen(true)}
          />
          <main
            ref={mainRef}
            className={cn("min-h-0 flex-1 overflow-y-auto", mainScrollPaddingClass)}
          >
            {children}
          </main>
        </div>
      </div>
      {hidePlayer ? null : <MiniPlayer />}
      <MobileDrawer
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        activePath={activePath}
      />
    </div>
  );
}
