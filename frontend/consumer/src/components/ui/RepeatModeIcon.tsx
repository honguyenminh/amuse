import {
  RepeatOneIcon,
  RepeatQueueIcon,
} from "@/components/ui/PlaybackIcons";
import type { RepeatMode } from "@/lib/playback/types";

type RepeatModeIconProps = {
  mode: RepeatMode;
  className?: string;
};

export function RepeatModeIcon({ mode, className }: RepeatModeIconProps) {
  if (mode === "one") {
    return <RepeatOneIcon className={className} />;
  }
  return <RepeatQueueIcon className={className} />;
}
