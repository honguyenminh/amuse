"use client";

import { PortalGate } from "@/components/portal/PortalGate";
import type { ReactNode } from "react";

export default function PortalLayout({ children }: { children: ReactNode }) {
  return <PortalGate>{children}</PortalGate>;
}
