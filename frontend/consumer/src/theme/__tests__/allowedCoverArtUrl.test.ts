import { afterEach, describe, expect, it, vi } from "vitest";
import { isAllowedCoverArtUrl } from "../allowedCoverArtUrl";

describe("isAllowedCoverArtUrl", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("allows URLs on the configured media public origin", () => {
    vi.stubEnv("MEDIA_PUBLIC_BASE_URL", "http://localhost:9000");
    expect(
      isAllowedCoverArtUrl(
        "http://localhost:9000/amuse-covers/releases/example.jpg",
      ),
    ).toBe(true);
  });

  it("rejects other origins", () => {
    vi.stubEnv("MEDIA_PUBLIC_BASE_URL", "http://localhost:9000");
    expect(isAllowedCoverArtUrl("http://evil.example/cover.jpg")).toBe(false);
  });

  it("rejects non-http schemes", () => {
    vi.stubEnv("MEDIA_PUBLIC_BASE_URL", "http://localhost:9000");
    expect(isAllowedCoverArtUrl("file:///etc/passwd")).toBe(false);
  });

  it("blocks metadata IP targets in production", () => {
    vi.stubEnv("NODE_ENV", "production");
    vi.stubEnv("MEDIA_PUBLIC_BASE_URL", "https://cdn.example.com");
    expect(isAllowedCoverArtUrl("http://169.254.169.254/latest/meta-data")).toBe(
      false,
    );
  });
});
