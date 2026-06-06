"use client";

import Link from "next/link";
import type { ReactNode } from "react";
import { catalogHashtagPath } from "./paths";
import { isAllowedLinkUrl, parseFormattedCatalogText } from "./parseFormattedCatalogText";
import type { InlineNode } from "./types";

export type FormattedCatalogTextProps = {
  text: string;
  className?: string;
  paragraphClassName?: string;
  codeClassName?: string;
  linkClassName?: string;
  hashtagClassName?: string;
};

export function FormattedCatalogText({
  text,
  className,
  paragraphClassName = "mb-2 last:mb-0",
  codeClassName = "rounded bg-surface-container-high px-1 font-mono text-sm",
  linkClassName = "text-sky-700 underline underline-offset-2 hover:text-sky-900 dark:text-sky-300 dark:hover:text-sky-200",
  hashtagClassName = "font-medium text-sky-700 underline underline-offset-2 hover:text-sky-900 dark:text-sky-300 dark:hover:text-sky-200",
}: FormattedCatalogTextProps) {
  const document = parseFormattedCatalogText(text);

  if (document.paragraphs.length === 0) {
    return null;
  }

  return (
    <div className={className}>
      {document.paragraphs.map((paragraph, index) => (
        <p key={index} className={paragraphClassName}>
          {renderInlineNodes(paragraph.children, {
            codeClassName,
            linkClassName,
            hashtagClassName,
          })}
        </p>
      ))}
    </div>
  );
}

function renderInlineNodes(
  nodes: InlineNode[],
  classes: {
    codeClassName: string;
    linkClassName: string;
    hashtagClassName: string;
  },
): ReactNode[] {
  return nodes.map((node, index) => renderInlineNode(node, index, classes));
}

function renderInlineNode(
  node: InlineNode,
  key: number,
  classes: {
    codeClassName: string;
    linkClassName: string;
    hashtagClassName: string;
  },
): ReactNode {
  switch (node.type) {
    case "text":
      return node.value;
    case "bold":
      return <strong key={key}>{renderInlineNodes(node.children, classes)}</strong>;
    case "italic":
      return <em key={key}>{renderInlineNodes(node.children, classes)}</em>;
    case "code":
      return (
        <code key={key} className={classes.codeClassName}>
          {node.value}
        </code>
      );
    case "link":
      if (!isAllowedLinkUrl(node.href)) {
        return (
          <span key={key}>[{renderInlineNodes(node.children, classes)}]({node.href})</span>
        );
      }
      return (
        <a
          key={key}
          href={node.href}
          target="_blank"
          rel="noopener noreferrer"
          className={classes.linkClassName}
        >
          {renderInlineNodes(node.children, classes)}
        </a>
      );
    case "hashtag":
      return (
        <Link key={key} href={catalogHashtagPath(node.tag)} className={classes.hashtagClassName}>
          #{node.tag}
        </Link>
      );
    default:
      return null;
  }
}

export type { ParagraphNode, InlineNode, FormattedCatalogTextDocument } from "./types";
