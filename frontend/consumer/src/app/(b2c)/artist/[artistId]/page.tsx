"use client";

import { AppShell } from "@/components/ui/AppShell";
import { Card } from "@/components/ui/Card";
import { Text } from "@/components/ui/Text";
import { DEMO_ARTIST_SEEDS } from "@/theme/defaultPalette";
import { usePageSeed } from "@/theme/ThemeProvider";
import { use } from "react";

export default function ArtistPage({
  params,
}: {
  params: Promise<{ artistId: string }>;
}) {
  const { artistId } = use(params);
  const seed = DEMO_ARTIST_SEEDS[artistId] ?? DEMO_ARTIST_SEEDS.demo;
  usePageSeed(seed);

  return (
    <AppShell title={`Artist ${artistId}`} activePath="/artist">
      <div className="flex flex-col gap-4 p-4">
        <Card>
          <Text variant="headline-medium">Artist page</Text>
          <Text variant="body-medium">
            Custom page seed active (shell follows this route). Seed hue ≈ {seed.h}°.
          </Text>
        </Card>
      </div>
    </AppShell>
  );
}
