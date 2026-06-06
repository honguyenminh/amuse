import Link from "@tiptap/extension-link";

export const CatalogTextLink = Link.extend({
  renderHTML({ mark, HTMLAttributes }) {
    const href = typeof mark.attrs.href === "string" ? mark.attrs.href : "";

    return [
      "a",
      {
        ...HTMLAttributes,
        href,
        title: href,
        class: "catalog-text-link",
      },
      0,
    ];
  },
}).configure({
  openOnClick: false,
  autolink: false,
  protocols: ["http", "https"],
});
