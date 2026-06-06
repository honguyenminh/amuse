"use client";

import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Text } from "@/components/ui/Text";
import { useEffect, useId, useRef, useState, type ReactNode } from "react";

type PromptDialogProps = {
  open: boolean;
  title: string;
  description?: ReactNode;
  label: string;
  placeholder?: string;
  defaultValue?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  confirmDisabled?: boolean;
  onClose: () => void;
  onConfirm: (value: string) => void;
};

export function PromptDialog({
  open,
  title,
  description,
  label,
  placeholder,
  defaultValue = "",
  confirmLabel = "OK",
  cancelLabel = "Cancel",
  confirmDisabled = false,
  onClose,
  onConfirm,
}: PromptDialogProps) {
  const titleId = useId();
  const descriptionId = useId();
  const inputId = useId();
  const [value, setValue] = useState(defaultValue);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (!open) return;
    setValue(defaultValue);
    const frame = requestAnimationFrame(() => {
      inputRef.current?.focus();
      inputRef.current?.select();
    });
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
  }, [open, defaultValue, onClose]);

  if (!open) return null;

  const trimmed = value.trim();
  const canConfirm = trimmed.length > 0 && !confirmDisabled;

  const submit = () => {
    if (!canConfirm) return;
    onConfirm(trimmed);
  };

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/40" onClick={onClose} />
      <Card
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={description ? descriptionId : undefined}
        className="relative z-10 w-full max-w-md shadow-2xl"
        onClick={(event) => event.stopPropagation()}
      >
        <h2 id={titleId} className="text-title-large text-on-surface">
          {title}
        </h2>
        {description ? (
          <div id={descriptionId} className="mt-2 text-on-surface-variant">
            {description}
          </div>
        ) : null}
        <label htmlFor={inputId} className="mt-4 block">
          <Text variant="label-medium">{label}</Text>
          <input
            ref={inputRef}
            id={inputId}
            type="text"
            value={value}
            placeholder={placeholder}
            disabled={confirmDisabled}
            className="mt-1 w-full rounded-md border-2 border-outline bg-background px-3 py-2 text-body-medium text-on-surface"
            onChange={(event) => setValue(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                event.preventDefault();
                submit();
              }
            }}
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
