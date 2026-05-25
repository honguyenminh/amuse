import type { ReactNode } from "react";
import { BottomNav } from "./BottomNav";
import { TopBar } from "./TopBar";

type AppShellProps = {
  title: string;
  activePath: string;
  trailing?: React.ReactNode;
  children: ReactNode;
};

export function AppShell({
  title,
  activePath,
  trailing,
  children,
}: AppShellProps) {
  return (
    <div className="flex min-h-full flex-col bg-background text-on-background">
      <TopBar title={title} trailing={trailing} />
      <main className="flex flex-1 flex-col">{children}</main>
      <BottomNav activePath={activePath} />
    </div>
  );
}
