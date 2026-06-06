export type InlineTextNode = { type: "text"; value: string };

export type InlineBoldNode = { type: "bold"; children: InlineNode[] };

export type InlineItalicNode = { type: "italic"; children: InlineNode[] };

export type InlineCodeNode = { type: "code"; value: string };

export type InlineLinkNode = { type: "link"; href: string; children: InlineNode[] };

export type InlineHashtagNode = { type: "hashtag"; tag: string };

export type InlineNode =
  | InlineTextNode
  | InlineBoldNode
  | InlineItalicNode
  | InlineCodeNode
  | InlineLinkNode
  | InlineHashtagNode;

export type ParagraphNode = { type: "paragraph"; children: InlineNode[] };

export type FormattedCatalogTextDocument = {
  paragraphs: ParagraphNode[];
};
