"use client";

import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Text } from "@/components/ui/Text";
import { useEffect, useId, useRef, useState } from "react";

export const MAX_PLAYLIST_DESCRIPTION_LENGTH = 100;

type PlaylistFormDialogProps = {
  open: boolean;
  title: string;
  initialTitle?: string;
  initialDescription?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  confirmDisabled?: boolean;
  onClose: () => void;
  onConfirm: (values: { title: string; description: string }) => void;
};

export function PlaylistFormDialog({
  open,
  title,
  initialTitle = "",
  initialDescription = "",
  confirmLabel = "Save",
  cancelLabel = "Cancel",
  confirmDisabled = false,
  onClose,
  onConfirm,
}: PlaylistFormDialogProps) {
  const titleId = useId();
  const nameInputId = useId();
  const descriptionInputId = useId();
  const [name, setName] = useState(initialTitle);
  const [description, setDescription] = useState(initialDescription);
  const nameInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (!open) return;
    setName(initialTitle);
    setDescription(initialDescription);
    const frame = requestAnimationFrame(() => nameInputRef.current?.focus());
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    document.addEventListener("keydown", onKey);
    const prev = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      cancelAnimationFrame(frame);
      document.removeEventListener("keydown", onKey);
      document.body.style.overflow = prev;
    };
  }, [open, initialTitle, initialDescription, onClose]);

  if (!open) return null;

  const trimmedName = name.trim();
  const canConfirm = trimmedName.length > 0 && !confirmDisabled;

  const submit = () => {
    if (!canConfirm) return;
    onConfirm({
      title: trimmedName,
      description: description.trim(),
    });
  };

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/40" onClick={onClose} />
      <Card
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="relative z-10 w-full max-w-md shadow-2xl"
        onClick={(event) => event.stopPropagation()}
      >
        <h2 id={titleId} className="text-title-large text-on-surface">
          {title}
        </h2>
        <label htmlFor={nameInputId} className="mt-4 block">
          <Text variant="label-medium">Name</Text>
          <input
            ref={nameInputRef}
            id={nameInputId}
            type="text"
            value={name}
            disabled={confirmDisabled}
            className="mt-1 w-full rounded-md border-2 border-outline bg-background px-3 py-2 text-body-medium text-on-surface"
            onChange={(event) => setName(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter" && !event.shiftKey) {
                event.preventDefault();
                submit();
              }
            }}
          />
        </label>
        <label htmlFor={descriptionInputId} className="mt-3 block">
          <div className="flex items-baseline justify-between gap-2">
            <Text variant="label-medium">Description (optional)</Text>
            <Text variant="label-small" className="text-on-surface-variant">
              {description.length}/{MAX_PLAYLIST_DESCRIPTION_LENGTH}
            </Text>
          </div>
          <textarea
            id={descriptionInputId}
            value={description}
            maxLength={MAX_PLAYLIST_DESCRIPTION_LENGTH}
            rows={3}
            disabled={confirmDisabled}
            placeholder="What's this playlist about?"
            className="mt-1 w-full resize-y rounded-md border-2 border-outline bg-background px-3 py-2 text-body-medium text-on-surface"
            onChange={(event) => setDescription(event.target.value)}
          />
        </label>
        <div className="mt-4 flex flex-wrap justify-end gap-2">
          <Button type="button" variant="outlined" onClick={onClose} disabled={confirmDisabled}>
            {cancelLabel}
          </Button>
          <Button type="button" disabled={!canConfirm} onClick={submit}>
            {confirmLabel}
          </Button>
        </div>
      </Card>
    </div>
  );
}
