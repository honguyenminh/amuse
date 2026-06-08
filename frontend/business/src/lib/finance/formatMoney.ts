export function formatMinor(amountMinor: number, currency: string): string {
  const major = amountMinor / 100;
  return `${major.toFixed(2)} ${currency}`;
}

export function parseMajorToMinor(value: string): number | null {
  const parsed = Number.parseFloat(value.trim());
  if (!Number.isFinite(parsed) || parsed < 0) {
    return null;
  }
  return Math.round(parsed * 100);
}
