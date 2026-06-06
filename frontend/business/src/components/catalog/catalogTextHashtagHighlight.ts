import { Extension } from "@tiptap/core";
import type { Node as ProseMirrorNode } from "@tiptap/pm/model";
import { Plugin, PluginKey, type EditorState } from "@tiptap/pm/state";
import { Decoration, DecorationSet } from "@tiptap/pm/view";

const HASHTAG_PATTERN = /#([a-zA-Z][a-zA-Z0-9_]{0,63})/g;

export const catalogTextHashtagPluginKey = new PluginKey("catalogTextHashtag");

function buildHashtagDecorations(doc: ProseMirrorNode) {
  const decorations: Decoration[] = [];

  doc.descendants((node: ProseMirrorNode, pos: number) => {
    if (!node.isText || !node.text) return;

    const text = node.text;
    HASHTAG_PATTERN.lastIndex = 0;
    let match: RegExpExecArray | null;
    while ((match = HASHTAG_PATTERN.exec(text)) !== null) {
      const index = match.index;
      if (index > 0 && /\w/.test(text[index - 1]!)) continue;

      const from = pos + index;
      const to = from + match[0].length;
      decorations.push(
        Decoration.inline(from, to, {
          class: "catalog-text-hashtag",
        }),
      );
    }
  });

  return DecorationSet.create(doc, decorations);
}

export const CatalogTextHashtagHighlight = Extension.create({
  name: "catalogTextHashtagHighlight",
  addProseMirrorPlugins() {
    return [
      new Plugin({
        key: catalogTextHashtagPluginKey,
        props: {
          decorations(state: EditorState) {
            return buildHashtagDecorations(state.doc);
          },
        },
      }),
    ];
  },
});
