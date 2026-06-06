"use client";

import {
  CATALOG_FORMATTED_TEXT_MAX_LENGTH,
  isValidHashtagTag,
} from "@amuse/catalog-text";
import { CatalogTextHashtagHighlight } from "@/components/catalog/catalogTextHashtagHighlight";
import { CatalogTextLink } from "@/components/catalog/catalogTextLinkExtension";
import { CatalogTextLinkDialog } from "@/components/catalog/CatalogTextLinkDialog";
import { Button } from "@/components/ui/button";
import { getMarkRange } from "@tiptap/core";
import Bold from "@tiptap/extension-bold";
import Code from "@tiptap/extension-code";
import Document from "@tiptap/extension-document";
import HardBreak from "@tiptap/extension-hard-break";
import History from "@tiptap/extension-history";
import Italic from "@tiptap/extension-italic";
import Paragraph from "@tiptap/extension-paragraph";
import Text from "@tiptap/extension-text";
import { Markdown } from "@tiptap/markdown";
import { EditorContent, useEditor } from "@tiptap/react";
import {
  Bold as BoldIcon,
  Code as CodeIcon,
  ExternalLink,
  Hash,
  Italic as ItalicIcon,
  Link as LinkIcon,
  Redo2,
  Undo2,
} from "lucide-react";
import type { ReactNode } from "react";
import { useCallback, useEffect, useMemo, useReducer, useState } from "react";

export type CatalogTextEditorProps = {
  value: string;
  onChange: (markdown: string) => void;
  disabled?: boolean;
  maxLength?: number;
  placeholder?: string;
  id?: string;
};

type LinkEditRange = {
  from: number;
  to: number;
};

type InlineMarkName = "bold" | "italic" | "code";

function isTypingMarkActive(
  editor: NonNullable<ReturnType<typeof useEditor>>,
  mark: InlineMarkName,
): boolean {
  const markType = editor.schema.marks[mark];
  if (!markType) return false;

  const { storedMarks, selection } = editor.state;
  const marks = storedMarks ?? selection.$from.marks();
  return marks.some((activeMark) => activeMark.type === markType);
}

function isToolbarMarkActive(
  editor: NonNullable<ReturnType<typeof useEditor>>,
  mark: InlineMarkName,
): boolean {
  if (editor.state.selection.empty) {
    return isTypingMarkActive(editor, mark);
  }
  return editor.isActive(mark);
}

function toggleInlineMark(
  editor: NonNullable<ReturnType<typeof useEditor>>,
  mark: InlineMarkName,
) {
  const markType = editor.schema.marks[mark];
  if (!markType) return;

  const { empty } = editor.state.selection;

  if (!empty) {
    editor.chain().focus().extendMarkRange(mark).toggleMark(mark).run();
    return;
  }

  if (isTypingMarkActive(editor, mark)) {
    const marks = (editor.state.storedMarks ?? editor.state.selection.$from.marks()).filter(
      (activeMark) => activeMark.type !== markType,
    );

    editor
      .chain()
      .focus()
      .command(({ tr, dispatch }) => {
        if (dispatch) {
          tr.removeStoredMark(markType);
          tr.setStoredMarks(marks);
        }
        return true;
      })
      .run();
    return;
  }

  editor.chain().focus().setMark(mark).run();
}

function ToolbarButton({
  label,
  active,
  disabled,
  onClick,
  children,
}: {
  label: string;
  active?: boolean;
  disabled?: boolean;
  onClick: () => void;
  children: ReactNode;
}) {
  return (
    <button
      type="button"
      aria-label={label}
      aria-pressed={active ?? false}
      title={label}
      disabled={disabled}
      onClick={onClick}
      onMouseDown={(event) => event.preventDefault()}
      className={[
        "inline-flex size-8 items-center justify-center rounded-md border border-input text-sm transition-colors",
        active ? "bg-accent text-accent-foreground" : "bg-transparent hover:bg-accent/50",
        disabled ? "cursor-not-allowed opacity-50" : "",
      ].join(" ")}
    >
      {children}
    </button>
  );
}

function getActiveLinkInfo(editor: NonNullable<ReturnType<typeof useEditor>>) {
  if (!editor.isActive("link")) return null;

  const href = editor.getAttributes("link").href;
  if (typeof href !== "string" || href.length === 0) return null;

  const range = getMarkRange(editor.state.selection.$from, editor.schema.marks.link);
  const label = range
    ? editor.state.doc.textBetween(range.from, range.to, "")
    : editor.state.doc.textBetween(
        editor.state.selection.from,
        editor.state.selection.to,
        "",
      );

  return { href, label, range };
}

export function CatalogTextEditorInner({
  value,
  onChange,
  disabled = false,
  maxLength = CATALOG_FORMATTED_TEXT_MAX_LENGTH,
  placeholder,
  id,
}: CatalogTextEditorProps) {
  const [, rerenderToolbar] = useReducer((count: number) => count + 1, 0);
  const [markdownSource, setMarkdownSource] = useState(value);
  const [showMarkdownSource, setShowMarkdownSource] = useState(false);
  const [linkDialogOpen, setLinkDialogOpen] = useState(false);
  const [linkDialogUrl, setLinkDialogUrl] = useState("https://");
  const [linkDialogLabel, setLinkDialogLabel] = useState("");
  const [linkDialogHasExisting, setLinkDialogHasExisting] = useState(false);
  const [linkEditRange, setLinkEditRange] = useState<LinkEditRange | null>(null);

  const extensions = useMemo(
    () => [
      Document,
      Paragraph,
      Text,
      HardBreak,
      History,
      Bold,
      Italic,
      Code.configure({
        HTMLAttributes: {
          class: "catalog-text-code",
        },
      }),
      CatalogTextLink,
      CatalogTextHashtagHighlight,
      Markdown,
    ],
    [],
  );

  const editor = useEditor({
    immediatelyRender: false,
    editable: !disabled,
    extensions,
    content: value || "",
    contentType: "markdown",
    editorProps: {
      attributes: {
        ...(id ? { id } : {}),
        class: "catalog-text-editor-content",
        "aria-placeholder": placeholder ?? "Write a description…",
        "data-gramm": "false",
        "data-enable-grammarly": "false",
      },
    },
    onUpdate: ({ editor: currentEditor }) => {
      const markdown = currentEditor.getMarkdown();
      setMarkdownSource(markdown);
      onChange(markdown);
      rerenderToolbar();
    },
    onSelectionUpdate: () => {
      rerenderToolbar();
    },
    onTransaction: () => {
      rerenderToolbar();
    },
  });

  useEffect(() => {
    if (!editor) return;
    editor.setEditable(!disabled);
  }, [disabled, editor]);

  useEffect(() => {
    if (!editor) return;
    const current = editor.getMarkdown();
    if (current !== value) {
      editor.commands.setContent(value || "", { contentType: "markdown", emitUpdate: false });
      setMarkdownSource(value);
    }
  }, [editor, value]);

  useEffect(() => {
    setMarkdownSource(value);
  }, [value]);

  const length = markdownSource.length;
  const activeLink = editor ? getActiveLinkInfo(editor) : null;

  const openLinkDialogForSelection = useCallback(
    (currentEditor: NonNullable<ReturnType<typeof useEditor>>, forcedRange?: LinkEditRange) => {
      const { from, to, empty } = currentEditor.state.selection;
      const active = getActiveLinkInfo(currentEditor);
      const range = forcedRange ?? active?.range ?? { from, to };
      const previousUrl =
        active?.href ?? (currentEditor.getAttributes("link").href as string | undefined);
      const selectedLabel =
        active?.label ??
        (empty ? "" : currentEditor.state.doc.textBetween(range.from, range.to, ""));

      setLinkDialogUrl(
        typeof previousUrl === "string" && previousUrl.length > 0 ? previousUrl : "https://",
      );
      setLinkDialogLabel(selectedLabel);
      setLinkDialogHasExisting(currentEditor.isActive("link") || Boolean(forcedRange));
      setLinkEditRange(range);
      setLinkDialogOpen(true);
    },
    [],
  );

  function openLinkDialog() {
    if (!editor) return;
    openLinkDialogForSelection(editor);
  }

  function applyLink(url: string, label: string) {
    if (!editor) return;

    const range = linkEditRange ?? {
      from: editor.state.selection.from,
      to: editor.state.selection.to,
    };

    editor
      .chain()
      .focus()
      .insertContentAt({ from: range.from, to: range.to }, label, { updateSelection: true })
      .setTextSelection({ from: range.from, to: range.from + label.length })
      .setLink({ href: url })
      .run();

    setLinkEditRange(null);
  }

  function removeLink() {
    if (!editor) return;
    const range = linkEditRange;
    if (range) {
      editor.chain().focus().setTextSelection(range).extendMarkRange("link").unsetLink().run();
    } else {
      editor.chain().focus().extendMarkRange("link").unsetLink().run();
    }
    setLinkEditRange(null);
    setLinkDialogOpen(false);
  }

  function applyHashtag() {
    if (!editor) return;
    const { from, to, empty } = editor.state.selection;

    if (empty) {
      editor.chain().focus().insertContent("#").run();
      return;
    }

    const selected = editor.state.doc.textBetween(from, to, "");
    const raw = selected.startsWith("#") ? selected.slice(1) : selected;
    const normalized = raw.trim().replace(/\s+/g, "");
    const tagMatch = normalized.match(/^[a-zA-Z][a-zA-Z0-9_]*/);
    const tag = tagMatch?.[0] ?? normalized;

    if (!tag || !isValidHashtagTag(tag)) {
      editor.chain().focus().insertContentAt({ from, to }, `#${normalized}`).run();
      return;
    }

    editor.chain().focus().insertContentAt({ from, to }, `#${tag}`).run();
  }

  function toggleMark(mark: InlineMarkName) {
    if (!editor) return;
    toggleInlineMark(editor, mark);
  }

  return (
    <div className="grid gap-2">
      <div className="flex flex-wrap gap-1">
        <ToolbarButton
          label="Undo"
          disabled={disabled || !editor?.can().undo()}
          onClick={() => editor?.chain().focus().undo().run()}
        >
          <Undo2 className="size-4" />
        </ToolbarButton>
        <ToolbarButton
          label="Redo"
          disabled={disabled || !editor?.can().redo()}
          onClick={() => editor?.chain().focus().redo().run()}
        >
          <Redo2 className="size-4" />
        </ToolbarButton>
        <ToolbarButton
          label="Bold"
          active={editor ? isToolbarMarkActive(editor, "bold") : false}
          disabled={disabled || !editor}
          onClick={() => toggleMark("bold")}
        >
          <BoldIcon className="size-4" />
        </ToolbarButton>
        <ToolbarButton
          label="Italic"
          active={editor ? isToolbarMarkActive(editor, "italic") : false}
          disabled={disabled || !editor}
          onClick={() => toggleMark("italic")}
        >
          <ItalicIcon className="size-4" />
        </ToolbarButton>
        <ToolbarButton
          label="Inline code"
          active={editor ? isToolbarMarkActive(editor, "code") : false}
          disabled={disabled || !editor}
          onClick={() => toggleMark("code")}
        >
          <CodeIcon className="size-4" />
        </ToolbarButton>
        <ToolbarButton
          label="Link"
          active={editor?.isActive("link") ?? false}
          disabled={disabled || !editor}
          onClick={openLinkDialog}
        >
          <LinkIcon className="size-4" />
        </ToolbarButton>
        <ToolbarButton
          label="Hashtag"
          disabled={disabled || !editor}
          onClick={applyHashtag}
        >
          <Hash className="size-4" />
        </ToolbarButton>
      </div>

      <div className="catalog-text-editor rounded-md border border-input bg-transparent shadow-xs focus-within:border-ring focus-within:ring-[3px] focus-within:ring-ring/50">
        <EditorContent editor={editor} />
      </div>

      {activeLink ? (
        <div className="flex flex-wrap items-center gap-2 rounded-md border border-sky-200/80 bg-sky-50/80 px-3 py-2 text-xs dark:border-sky-900/60 dark:bg-sky-950/40">
          <LinkIcon className="size-3.5 shrink-0 text-sky-700 dark:text-sky-300" />
          <div className="min-w-0 flex-1">
            <p className="truncate font-medium text-foreground">{activeLink.label || "Link"}</p>
            <p className="truncate text-sky-700 dark:text-sky-300" title={activeLink.href}>
              {activeLink.href}
            </p>
          </div>
          <Button type="button" size="sm" variant="outline" onClick={openLinkDialog}>
            Edit link
          </Button>
          <a
            href={activeLink.href}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex size-8 items-center justify-center rounded-md border border-input text-sky-700 hover:bg-accent/50 dark:text-sky-300"
            aria-label="Open link in new tab"
            title="Open link"
          >
            <ExternalLink className="size-3.5" />
          </a>
        </div>
      ) : null}

      <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-muted-foreground">
        <span>Visual editor — stored as markdown. Use Ctrl/Cmd+Z to undo.</span>
        <span className={length > maxLength ? "text-destructive" : undefined}>
          {length}/{maxLength}
        </span>
      </div>

      <div className="rounded-md border border-input">
        <button
          type="button"
          className="flex w-full items-center justify-between px-3 py-2 text-left text-sm font-medium"
          aria-expanded={showMarkdownSource}
          onClick={() => setShowMarkdownSource((open) => !open)}
        >
          Markdown source
          <span className="text-xs font-normal text-muted-foreground">
            {showMarkdownSource ? "Hide" : "Show"}
          </span>
        </button>
        {showMarkdownSource ? (
          <pre className="max-h-40 overflow-auto border-t border-input bg-muted/30 px-3 py-2 font-mono text-xs leading-relaxed whitespace-pre-wrap break-words text-foreground">
            {markdownSource.trim().length > 0 ? markdownSource : "(empty)"}
          </pre>
        ) : null}
      </div>

      <CatalogTextLinkDialog
        open={linkDialogOpen}
        onOpenChange={(open) => {
          setLinkDialogOpen(open);
          if (!open) setLinkEditRange(null);
        }}
        initialUrl={linkDialogUrl}
        initialLabel={linkDialogLabel}
        hasExistingLink={linkDialogHasExisting}
        onApply={applyLink}
        onRemove={linkDialogHasExisting ? removeLink : undefined}
      />
    </div>
  );
}
