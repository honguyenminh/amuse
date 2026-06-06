import type {
  FormattedCatalogTextDocument,
  InlineNode,
  ParagraphNode,
} from "./types";

const HASHTAG_INLINE = /^#([a-zA-Z][a-zA-Z0-9_]{0,63})/;

export function isAllowedLinkUrl(url: string): boolean {
  try {
    const parsed = new URL(url);
    return parsed.protocol === "http:" || parsed.protocol === "https:";
  } catch {
    return false;
  }
}

export function parseFormattedCatalogText(raw: string): FormattedCatalogTextDocument {
  const normalized = raw.replace(/\r\n/g, "\n").replace(/\r/g, "\n");
  const lines = normalized.split("\n");
  const paragraphs: ParagraphNode[] = lines.map((line) => ({
    type: "paragraph",
    children: parseInline(line),
  }));
  return { paragraphs };
}

function parseInline(input: string): InlineNode[] {
  const nodes: InlineNode[] = [];
  let index = 0;

  while (index < input.length) {
    if (input[index] === "`") {
      const end = input.indexOf("`", index + 1);
      if (end !== -1) {
        nodes.push({ type: "code", value: input.slice(index + 1, end) });
        index = end + 1;
        continue;
      }
    }

    if (input[index] === "[") {
      const link = tryParseLink(input, index);
      if (link) {
        nodes.push(link.node);
        index = link.nextIndex;
        continue;
      }
    }

    if (input.startsWith("**", index)) {
      const close = findClosingDelimiter(input, index + 2, "**");
      if (close !== -1) {
        nodes.push({
          type: "bold",
          children: parseInline(input.slice(index + 2, close)),
        });
        index = close + 2;
        continue;
      }
    }

    if (input.startsWith("__", index)) {
      const close = findClosingDelimiter(input, index + 2, "__");
      if (close !== -1) {
        nodes.push({
          type: "bold",
          children: parseInline(input.slice(index + 2, close)),
        });
        index = close + 2;
        continue;
      }
    }

    if (input[index] === "*" && input[index + 1] !== "*") {
      const close = findClosingSingleDelimiter(input, index + 1, "*");
      if (close !== -1) {
        nodes.push({
          type: "italic",
          children: parseInline(input.slice(index + 1, close)),
        });
        index = close + 1;
        continue;
      }
    }

    if (input[index] === "_" && input[index + 1] !== "_") {
      const close = findClosingSingleDelimiter(input, index + 1, "_");
      if (close !== -1) {
        nodes.push({
          type: "italic",
          children: parseInline(input.slice(index + 1, close)),
        });
        index = close + 1;
        continue;
      }
    }

    if (input[index] === "#" && (index === 0 || !/\w/.test(input[index - 1]!))) {
      const rest = input.slice(index);
      const match = HASHTAG_INLINE.exec(rest);
      if (match) {
        nodes.push({ type: "hashtag", tag: match[1]! });
        index += match[0].length;
        continue;
      }
    }

    let next = index + 1;
    while (next < input.length) {
      const char = input[next]!;
      if (char === "`" || char === "[" || char === "*" || char === "_" || char === "#") {
        break;
      }
      next++;
    }

    nodes.push({ type: "text", value: input.slice(index, next) });
    index = next;
  }

  return mergeAdjacentTextNodes(nodes);
}

function tryParseLink(
  input: string,
  start: number,
): { node: InlineNode; nextIndex: number } | null {
  const labelEnd = input.indexOf("]", start + 1);
  if (labelEnd === -1 || input[labelEnd + 1] !== "(") return null;

  const urlEnd = input.indexOf(")", labelEnd + 2);
  if (urlEnd === -1) return null;

  const label = input.slice(start + 1, labelEnd);
  const href = input.slice(labelEnd + 2, urlEnd);

  return {
    node: {
      type: "link",
      href,
      children: parseInline(label),
    },
    nextIndex: urlEnd + 1,
  };
}

function findClosingDelimiter(input: string, from: number, delimiter: string): number {
  const close = input.indexOf(delimiter, from);
  return close;
}

function findClosingSingleDelimiter(input: string, from: number, delimiter: string): number {
  for (let i = from; i < input.length; i++) {
    if (input[i] === delimiter) return i;
  }
  return -1;
}

function mergeAdjacentTextNodes(nodes: InlineNode[]): InlineNode[] {
  const merged: InlineNode[] = [];
  for (const node of nodes) {
    const last = merged[merged.length - 1];
    if (node.type === "text" && last?.type === "text") {
      last.value += node.value;
    } else {
      merged.push(node);
    }
  }
  return merged;
}
