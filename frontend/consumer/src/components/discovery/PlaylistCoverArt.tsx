"use client";

import { cn } from "@/lib/cn";

type PlaylistCoverArtProps = {
  coverArtUrls: string[];
  className?: string;
  /**
   * - tile: fills the width of a grid card (library)
   * - hero: fixed square for playlist detail header
   * - row: compact square for search list rows
   */
  variant?: "tile" | "hero" | "row";
};

/** Index 0 = oldest (front of stack), 1 = second, 2 = third (back). Positions unchanged. */
const tileLayouts: Record<number, Array<{ className: string; z: string }>> = {
  1: [{ className: "left-[8%] top-[8%] h-[84%] w-[84%] rotate-0", z: "z-0" }],
  2: [
    { className: "left-[6%] top-[10%] h-[68%] w-[68%] -rotate-6", z: "z-10" },
    { className: "bottom-[8%] right-[6%] h-[68%] w-[68%] rotate-4", z: "z-0" },
  ],
  3: [
    { className: "left-[4%] top-[14%] h-[62%] w-[62%] -rotate-10", z: "z-20" },
    { className: "left-[18%] top-[8%] h-[68%] w-[68%] rotate-0", z: "z-10" },
    { className: "bottom-[8%] right-[4%] h-[62%] w-[62%] rotate-10", z: "z-0" },
  ],
};

const rowLayouts: Record<number, Array<{ className: string; z: string }>> = {
  1: [{ className: "inset-0 h-full w-full rotate-0", z: "z-0" }],
  2: [
    { className: "left-0 top-[8%] h-[78%] w-[78%] -rotate-6", z: "z-10" },
    { className: "bottom-[8%] right-0 h-[78%] w-[78%] rotate-4", z: "z-0" },
  ],
  3: [
    { className: "left-0 top-[12%] h-[72%] w-[72%] -rotate-8", z: "z-20" },
    { className: "left-[14%] top-[4%] h-[78%] w-[78%] rotate-0", z: "z-10" },
    { className: "bottom-[4%] right-0 h-[72%] w-[72%] rotate-8", z: "z-0" },
  ],
};

const variantShell: Record<NonNullable<PlaylistCoverArtProps["variant"]>, string> = {
  tile: "aspect-square w-full max-w-full min-h-0 min-w-0",
  hero: "aspect-square h-40 w-40 max-h-40 max-w-40 shrink-0 self-start",
  row: "size-12 max-h-12 max-w-12 shrink-0",
};

export function PlaylistCoverArt({
  coverArtUrls,
  className,
  variant = "tile",
}: PlaylistCoverArtProps) {
  const covers = coverArtUrls.slice(0, 3);
  const count = Math.min(Math.max(covers.length, 0), 3) as 0 | 1 | 2 | 3;
  const layouts = variant === "row" ? rowLayouts : tileLayouts;

  return (
    <div
      className={cn(
        "relative overflow-hidden rounded-md bg-surface-container-high",
        variantShell[variant],
        className,
      )}
    >
      {count === 0 ? (
        <div className="absolute inset-0 bg-gradient-to-br from-surface-container-high to-surface-variant" />
      ) : (
        layouts[count]?.map((layout, index) => (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            key={`${covers[index]}-${index}`}
            src={covers[index]}
            alt=""
            className={cn(
              "absolute max-h-full max-w-full rounded object-cover shadow-md ring-1 ring-outline/30",
              layout.className,
              layout.z,
            )}
          />
        ))
      )}
    </div>
  );
}
