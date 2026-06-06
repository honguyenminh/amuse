import { B2cGate } from "@/components/B2cGate";
import type { ReactNode } from "react";
import { Suspense } from "react";

export default function B2cLayout({ children }: { children: ReactNode }) {
  return (
    <Suspense
      fallback={
        <div className="flex h-dvh items-center justify-center bg-background p-8">
          Loading…
        </div>
      }
    >
      <B2cGate>{children}</B2cGate>
    </Suspense>
  );
}
