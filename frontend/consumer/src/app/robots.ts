import type { MetadataRoute } from "next";
import { absoluteUrl } from "@/lib/seo/siteUrl";

export default function robots(): MetadataRoute.Robots {
  return {
    rules: [
      {
        userAgent: "*",
        allow: ["/", "/home", "/artist/", "/playlist/", "/search"],
        disallow: ["/library", "/settings", "/auth", "/onboarding", "/playing"],
      },
    ],
    sitemap: absoluteUrl("/sitemap.xml"),
  };
}
