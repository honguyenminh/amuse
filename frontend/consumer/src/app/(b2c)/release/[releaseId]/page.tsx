"use client";

import { ReleasePageView } from "@/components/catalog/ReleasePageView";
import { getCatalogRelease } from "@/lib/api/catalogClient";
import { use, useCallback } from "react";

export default function ReleaseByIdPage({
  params,
}: {
  params: Promise<{ releaseId: string }>;
}) {
  const { releaseId } = use(params);
  const load = useCallback(() => getCatalogRelease(releaseId), [releaseId]);

  return <ReleasePageView loadKey={releaseId} load={load} />;
}
