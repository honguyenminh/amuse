"use client";

import { LibraryAuthGate } from "@/components/discovery/LibraryAuthGate";
import { LibraryTabs } from "@/components/discovery/LibraryTabs";
import { AppShell } from "@/components/ui/AppShell";
import { PageContent } from "@/components/ui/PageContent";
import type { ReactNode } from "react";

export default function LibraryLayout({ children }: { children: ReactNode }) {
  return (
    <AppShell title="Library" activePath="/library">
      <PageContent gap="6">
        <LibraryAuthGate>
          <LibraryTabs />
          {children}
        </LibraryAuthGate>
      </PageContent>
    </AppShell>
  );
}
