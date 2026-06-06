import { describe, expect, it } from "vitest";
import { catalogHashtagPath, isValidHashtagTag, normalizeHashtagTag } from "../paths";
import { isAllowedLinkUrl, parseFormattedCatalogText } from "../parseFormattedCatalogText";

describe("parseFormattedCatalogText", () => {
  it("parses plain text as a single paragraph", () => {
    const doc = parseFormattedCatalogText("Hello world");
    expect(doc.paragraphs).toHaveLength(1);
    expect(doc.paragraphs[0]?.children).toEqual([{ type: "text", value: "Hello world" }]);
  });

  it("preserves multiple lines as separate paragraphs", () => {
    const doc = parseFormattedCatalogText("Line one\nLine two");
    expect(doc.paragraphs).toHaveLength(2);
    expect(doc.paragraphs[0]?.children[0]).toEqual({ type: "text", value: "Line one" });
    expect(doc.paragraphs[1]?.children[0]).toEqual({ type: "text", value: "Line two" });
  });

  it("parses bold, italic, and code", () => {
    const doc = parseFormattedCatalogText("**bold** *italic* `code`");
    const children = doc.paragraphs[0]?.children ?? [];
    expect(children[0]).toEqual({ type: "bold", children: [{ type: "text", value: "bold" }] });
    expect(children[1]).toEqual({ type: "text", value: " " });
    expect(children[2]).toEqual({ type: "italic", children: [{ type: "text", value: "italic" }] });
    expect(children[3]).toEqual({ type: "text", value: " " });
    expect(children[4]).toEqual({ type: "code", value: "code" });
  });

  it("parses markdown links", () => {
    const doc = parseFormattedCatalogText("[Amuse](https://example.com)");
    expect(doc.paragraphs[0]?.children[0]).toEqual({
      type: "link",
      href: "https://example.com",
      children: [{ type: "text", value: "Amuse" }],
    });
  });

  it("parses hashtags but not mid-word hash fragments", () => {
    const doc = parseFormattedCatalogText("Tagged #electronic and foo#bar");
    const children = doc.paragraphs[0]?.children ?? [];
    expect(children).toContainEqual({ type: "text", value: "Tagged " });
    expect(children).toContainEqual({ type: "hashtag", tag: "electronic" });
    expect(children).toContainEqual({ type: "text", value: " and foo#bar" });
  });

  it("does not treat hash-space as hashtag", () => {
    const doc = parseFormattedCatalogText("# not a tag");
    expect(doc.paragraphs[0]?.children).toEqual([{ type: "text", value: "# not a tag" }]);
  });
});

describe("isAllowedLinkUrl", () => {
  it("allows http and https", () => {
    expect(isAllowedLinkUrl("https://example.com")).toBe(true);
    expect(isAllowedLinkUrl("http://example.com/path")).toBe(true);
  });

  it("rejects javascript and data urls", () => {
    expect(isAllowedLinkUrl("javascript:alert(1)")).toBe(false);
    expect(isAllowedLinkUrl("data:text/html,hello")).toBe(false);
  });
});

describe("catalogHashtagPath", () => {
  it("lowercases and encodes tags", () => {
    expect(catalogHashtagPath("Electronic")).toBe("/hashtag/electronic");
    expect(catalogHashtagPath("abc_123")).toBe("/hashtag/abc_123");
  });

  it("validates hashtag tags", () => {
    expect(isValidHashtagTag("abc")).toBe(true);
    expect(isValidHashtagTag("123")).toBe(false);
    expect(normalizeHashtagTag("MyTag")).toBe("mytag");
  });
});
