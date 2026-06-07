/** Plain-text excerpt for meta descriptions (strips lightweight markup noise). */
export function excerptText(value: string | null | undefined, maxLength = 160): string | undefined {
  if (!value?.trim()) return undefined;

  const plain = value
    .replace(/\[([^\]]+)\]\([^)]+\)/g, "$1")
    .replace(/[*_~`>#]/g, "")
    .replace(/\s+/g, " ")
    .trim();

  if (plain.length <= maxLength) return plain;
  return `${plain.slice(0, maxLength - 1).trimEnd()}…`;
}
