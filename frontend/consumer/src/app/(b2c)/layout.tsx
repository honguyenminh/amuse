import { B2cGate } from "@/components/B2cGate";
import type { ReactNode } from "react";

export default function B2cLayout({ children }: { children: ReactNode }) {
  return <B2cGate>{children}</B2cGate>;
}
