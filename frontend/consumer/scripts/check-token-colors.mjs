#!/usr/bin/env node
import { readFileSync, readdirSync, statSync } from "node:fs";
import { join } from "node:path";

const root = new URL("../src", import.meta.url).pathname;
const allowFiles = new Set([
  join(root, "app/globals.css"),
  join(root, "theme/defaultPalette.ts"),
  join(root, "theme/seedToPalette.ts"),
]);

const patterns = [
  /\b(bg|text|border|from|to|via)-(?:zinc|slate|gray|neutral|stone|red|blue|green)-/,
  /#[0-9a-fA-F]{3,8}\b/,
  /\brgb\(/,
  /\brgba\(/,
  /\bhsl\(/,
];

function walk(dir, files = []) {
  for (const name of readdirSync(dir)) {
    const path = join(dir, name);
    if (statSync(path).isDirectory()) {
      if (name === "__tests__" || name === "node_modules") continue;
      walk(path, files);
    } else if (/\.(tsx?|css)$/.test(name)) {
      files.push(path);
    }
  }
  return files;
}

const violations = [];
for (const file of walk(root)) {
  if (allowFiles.has(file)) continue;
  const content = readFileSync(file, "utf8");
  for (const pattern of patterns) {
    if (pattern.test(content)) {
      violations.push({ file, pattern: pattern.toString() });
      break;
    }
  }
}

if (violations.length > 0) {
  console.error("Token color violations:");
  for (const v of violations) {
    console.error(`  ${v.file} matched ${v.pattern}`);
  }
  process.exit(1);
}

console.log("No forbidden literal color usage outside token sources.");
