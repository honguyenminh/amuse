import { defineConfig } from "vitest/config";
import path from "node:path";

export default defineConfig({
  test: {
    environment: "node",
    server: {
      deps: {
        inline: ["@material/material-color-utilities"],
      },
    },
    include: [
      "src/**/__tests__/**/*.test.ts",
      "../packages/catalog-text/src/**/__tests__/**/*.test.ts",
    ],
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
});
