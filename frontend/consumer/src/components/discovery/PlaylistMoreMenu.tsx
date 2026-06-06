"use client";

import { AnchoredPopup } from "@/components/ui/AnchoredPopup";
import { OverflowMenuButton } from "@/components/ui/OverflowMenuButton";
import { Text } from "@/components/ui/Text";
import { cn } from "@/lib/cn";
import { useRef, useState } from "react";

type PlaylistMoreMenuProps = {
  reorderMode: boolean;
  onReorderModeChange: (enabled: boolean) => void;
  visibility: string;
  canEdit: boolean;
  showEditDetails?: boolean;
  isDeletable: boolean;
  busy?: boolean;
  onToggleVisibility: () => void;
  onEditShares: () => void;
  onEditDetails: () => void;
  onDelete: () => void;
};

function MenuItem({
  label,
  destructive,
  disabled,
  onClick,
}: {
  label: string;
  destructive?: boolean;
  disabled?: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      disabled={disabled}
      className={cn(
        "flex w-full px-4 py-2 text-left hover:bg-surface-variant disabled:cursor-not-allowed disabled:opacity-50",
        destructive && "text-error",
      )}
      onClick={onClick}
    >
      <Text variant="body-medium">{label}</Text>
    </button>
  );
}

export function PlaylistMoreMenu({
  reorderMode,
  onReorderModeChange,
  visibility,
  canEdit,
  showEditDetails = true,
  isDeletable,
  busy = false,
  onToggleVisibility,
  onEditShares,
  onEditDetails,
  onDelete,
}: PlaylistMoreMenuProps) {
  const [open, setOpen] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);

  const close = () => setOpen(false);

  return (
    <>
      <OverflowMenuButton
        ref={triggerRef}
        label="Playlist options"
        active={reorderMode}
        onClick={() => setOpen((value) => !value)}
      />
      <AnchoredPopup
        open={open}
        onClose={close}
        anchorRef={triggerRef}
        preferredPlacement="bottom"
        align="end"
        className="min-w-[12rem] rounded-md border-2 border-outline bg-surface py-1 shadow-lg"
      >
        {showEditDetails ? (
          <MenuItem
            label="Edit details"
            disabled={!canEdit || busy}
            onClick={() => {
              close();
              onEditDetails();
            }}
          />
        ) : null}
        <MenuItem
          label={reorderMode ? "Done reordering" : "Reorder tracks"}
          disabled={!canEdit || busy}
          onClick={() => {
            onReorderModeChange(!reorderMode);
            close();
          }}
        />
        <MenuItem
          label={visibility === "public" ? "Make private" : "Make public"}
          disabled={!canEdit || busy}
          onClick={() => {
            close();
            onToggleVisibility();
          }}
        />
        {visibility === "private" ? (
          <MenuItem
            label="Edit shares"
            disabled={!canEdit || busy}
            onClick={() => {
              close();
              onEditShares();
            }}
          />
        ) : null}
        {isDeletable ? (
          <MenuItem
            label="Delete playlist"
            destructive
            disabled={busy}
            onClick={() => {
              close();
              onDelete();
            }}
          />
        ) : null}
      </AnchoredPopup>
    </>
  );
}
