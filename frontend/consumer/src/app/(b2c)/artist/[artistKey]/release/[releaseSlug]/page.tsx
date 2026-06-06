"use client";

import { ReleasePageView } from "@/components/catalog/ReleasePageView";
import { getCatalogReleaseBySlugs } from "@/lib/api/catalogClient";
import { use, useCallback } from "react";

export default function ReleaseBySlugPage({
  params,
}: {
  params: Promise<{ artistKey: string; releaseSlug: string }>;
}) {
  const { artistKey, releaseSlug } = use(params);
  const loadKey = `${artistKey}/${releaseSlug}`;
  const load = useCallback(
    () => getCatalogReleaseBySlugs(artistKey, releaseSlug),
    [artistKey, releaseSlug],
  );

  return <ReleasePageView loadKey={loadKey} load={load} />;
}
