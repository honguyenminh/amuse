"use client";

import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Text } from "@/components/ui/Text";
import { DEFAULT_APP_SEED } from "@/theme/defaultPalette";
import { usePlaybackControls } from "@/theme/ThemeProvider";
import type { ColorSeed } from "@/theme/types";

const DEMO_TRACK_SEED: ColorSeed = { l: 0.55, c: 0.28, h: 200 };

export function PlaybackDemo() {
  const { play, pause, resume, isPaused, playingSeed } = usePlaybackControls();

  return (
    <Card className="flex flex-col gap-3">
      <Text variant="title-medium">Playback theme demo</Text>
      <Text variant="body-medium">
        {playingSeed
          ? `Playing seed: L=${playingSeed.l.toFixed(2)} C=${playingSeed.c.toFixed(2)} H=${playingSeed.h}`
          : `Idle — default seed L=${DEFAULT_APP_SEED.l} C=${DEFAULT_APP_SEED.c}`}
        {isPaused ? " (paused — faded palette)" : ""}
      </Text>
      <div className="flex flex-wrap gap-2">
        <Button type="button" onClick={() => play(DEMO_TRACK_SEED)}>
          Play demo track
        </Button>
        <Button type="button" variant="outlined" onClick={pause} disabled={!playingSeed}>
          Pause
        </Button>
        <Button type="button" variant="outlined" onClick={resume} disabled={!playingSeed || !isPaused}>
          Resume
        </Button>
      </div>
    </Card>
  );
}
