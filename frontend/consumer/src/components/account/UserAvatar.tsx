"use client";

import { avatarAccentClass, resolveInitials } from "@/lib/account/avatarAccent";
import { cn } from "@/lib/cn";

type UserAvatarProps = {
  displayName?: string | null;
  email?: string | null;
  accentSeed?: number | null;
  avatarUrl?: string | null;
  size?: "sm" | "md" | "lg";
  className?: string;
};

const sizeClasses = {
  sm: "size-8 text-xs leading-none tracking-normal",
  md: "size-10 text-sm leading-none tracking-normal",
  lg: "size-14 text-base leading-none tracking-normal",
} as const;

export function UserAvatar({
  displayName,
  email,
  accentSeed,
  avatarUrl,
  size = "md",
  className,
}: UserAvatarProps) {
  if (avatarUrl) {
    return (
      <img
        src={avatarUrl}
        alt=""
        className={cn(
          "inline-flex shrink-0 rounded-full object-cover",
          sizeClasses[size],
          className,
        )}
      />
    );
  }

  return (
    <span
      className={cn(
        "grid shrink-0 place-items-center rounded-full font-semibold",
        avatarAccentClass(accentSeed),
        sizeClasses[size],
        className,
      )}
      aria-hidden
    >
      {resolveInitials(displayName, email)}
    </span>
  );
}
