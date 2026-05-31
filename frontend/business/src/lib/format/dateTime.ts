export function formatDateTime(value: string | null | undefined): string {
  if (!value) {
    return "—";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "—";
  }

  return date.toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

export type TableDateTimeParts = {
  date: string;
  time: string;
  iso: string;
  title: string;
};

export function formatTableDateTimeParts(
  value: string | null | undefined,
): TableDateTimeParts | null {
  if (!value) {
    return null;
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return null;
  }

  return {
    date: date.toLocaleDateString(undefined, {
      month: "short",
      day: "numeric",
      year: "numeric",
    }),
    time: date.toLocaleTimeString(undefined, {
      timeStyle: "short",
    }),
    iso: value,
    title: formatDateTime(value),
  };
}
