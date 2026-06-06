"use client";

import dynamic from "next/dynamic";
import type { CatalogTextEditorProps } from "./CatalogTextEditorInner";

export type { CatalogTextEditorProps };

export const CatalogTextEditor = dynamic(
  () => import("./CatalogTextEditorInner").then((mod) => mod.CatalogTextEditorInner),
  {
    ssr: false,
    loading: () => (
      <div className="min-h-32 rounded-md border border-input bg-muted/20 px-3 py-2 text-sm text-muted-foreground">
        Loading editor…
      </div>
    ),
  },
);
