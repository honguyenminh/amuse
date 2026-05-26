import type { ReactNode } from "react";
import { MiniPlayer } from "@/components/player/MiniPlayer";
import { BottomNav } from "./BottomNav";
import { TopBar } from "./TopBar";

type AppShellProps = {
  title: string;
  activePath: string;
  trailing?: React.ReactNode;
  children: ReactNode;
  /** Hide the mini player on routes that own playback chrome themselves (e.g. /playing). */
  hidePlayer?: boolean;
};

export function AppShell({
  title,
  activePath,
  trailing,
  children,
  hidePlayer = false,
}: AppShellProps) {
  return (
    <div className="flex min-h-full flex-col bg-background text-on-background">
      <TopBar title={title} trailing={trailing} />
      <main className="flex flex-1 flex-col">{children}</main>
      {hidePlayer ? null : <MiniPlayer />}
      <BottomNav activePath={activePath} />
    </div>
  );
}
