import { PlaylistVisibilityIcon } from "@/components/ui/VisibilityIcons";
import { Text } from "@/components/ui/Text";
import { cn } from "@/lib/cn";

type PlaylistVisibilityLabelProps = {
  visibility: string;
  /** Prefix before the visibility phrase, e.g. "Liked · ". */
  prefix?: string;
  className?: string;
};

export function PlaylistVisibilityLabel({
  visibility,
  prefix,
  className,
}: PlaylistVisibilityLabelProps) {
  const label =
    visibility === "public" ? "Public playlist" : "Private playlist";
  const text = prefix ? `${prefix}${label.toLowerCase()}` : label;

  return (
    <span className={cn("inline-flex items-center gap-1.5", className)}>
      <PlaylistVisibilityIcon visibility={visibility} className="shrink-0" />
      <Text variant="label-medium" className="text-on-surface-variant">
        {text}
      </Text>
    </span>
  );
}
