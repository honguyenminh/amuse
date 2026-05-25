"use client";

import { AppShell } from "@/components/ui/AppShell";
import { Card } from "@/components/ui/Card";
import { Text } from "@/components/ui/Text";
import { DEMO_ALBUM_SEEDS } from "@/theme/defaultPalette";
import { usePageSeed } from "@/theme/ThemeProvider";
import { use } from "react";

export default function AlbumPage({
  params,
}: {
  params: Promise<{ albumId: string }>;
}) {
  const { albumId } = use(params);
  const seed = DEMO_ALBUM_SEEDS[albumId] ?? DEMO_ALBUM_SEEDS.demo;
  usePageSeed(seed);

  return (
    <AppShell title={`Album ${albumId}`} activePath="/album">
      <div className="flex flex-col gap-4 p-4">
        <Card>
          <Text variant="headline-medium">Album page</Text>
          <Text variant="body-medium">
            Custom page seed active (shell follows this route). Seed hue ≈ {seed.h}°.
          </Text>
        </Card>
      </div>
    </AppShell>
  );
}
