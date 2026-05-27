"use client";

import { Sidebar } from "@/components/ui/Sidebar";
import { cn } from "@/lib/cn";
import { useEffect } from "react";

type MobileDrawerProps = {
  open: boolean;
  onClose: () => void;
  activePath: string;
};

export function MobileDrawer({ open, onClose, activePath }: MobileDrawerProps) {
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    document.addEventListener("keydown", onKey);
    const prev = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.removeEventListener("keydown", onKey);
      document.body.style.overflow = prev;
    };
  }, [open, onClose]);

  return (
    <div
      className={cn(
        "fixed inset-0 z-50 md:hidden",
        open ? "pointer-events-auto" : "pointer-events-none",
      )}
      aria-hidden={!open}
    >
      <div
        className={cn(
          "absolute inset-0 bg-black/40 transition-opacity duration-200",
          open ? "opacity-100" : "opacity-0",
        )}
        onClick={onClose}
      />
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Navigation"
        className={cn(
          "absolute inset-y-0 left-0 flex h-full w-[min(18rem,85vw)] flex-col bg-surface shadow-2xl transition-transform duration-200",
          open ? "translate-x-0" : "-translate-x-full",
        )}
      >
        <Sidebar
          variant="drawer"
          activePath={activePath}
          onNavigate={onClose}
          onClose={onClose}
        />
      </div>
    </div>
  );
}
