import { describe, expect, it } from "vitest";
import { ApiError } from "@/lib/api/types";
import {
  isForbiddenError,
  isNotFoundError,
  shouldFallbackToClientFetch,
} from "@/lib/api/errors";

describe("isNotFoundError", () => {
  it("matches catalog 400 and discovery 404", () => {
    expect(isNotFoundError(new ApiError("missing", 400))).toBe(true);
    expect(isNotFoundError(new ApiError("missing", 404))).toBe(true);
    expect(isNotFoundError(new ApiError("forbidden", 403))).toBe(false);
  });
});

describe("shouldFallbackToClientFetch", () => {
  it("matches 403 for private playlist client hydration", () => {
    expect(shouldFallbackToClientFetch(new ApiError("forbidden", 403))).toBe(true);
    expect(isForbiddenError(new ApiError("forbidden", 403))).toBe(true);
    expect(shouldFallbackToClientFetch(new ApiError("missing", 404))).toBe(false);
  });
});
