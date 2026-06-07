import type { MetadataRoute } from "next";
import {
  fetchCatalogSitemap,
  type SitemapEntryDto,
} from "@/lib/api/catalogServer";
import {
  catalogArtistPath,
  catalogReleaseGroupPath,
  catalogReleasePath,
} from "@/lib/catalog/paths";
import { playlistPath } from "@/lib/discovery/paths";
import { absoluteUrl } from "@/lib/seo/siteUrl";

export const dynamic = "force-dynamic";
export const revalidate = 86400;

function entryUrl(entry: SitemapEntryDto): string {
  switch (entry.type) {
    case "artist":
      return absoluteUrl(catalogArtistPath(entry.artistSlug));
    case "release":
      return absoluteUrl(catalogReleasePath(entry.artistSlug, entry.releaseSlug));
    case "releaseGroup":
      return absoluteUrl(catalogReleaseGroupPath(entry.artistSlug, entry.groupSlug));
    case "playlist":
      return absoluteUrl(playlistPath(entry.playlistId));
  }
}

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const urls: MetadataRoute.Sitemap = [
    {
      url: absoluteUrl("/home"),
      changeFrequency: "daily",
      priority: 1,
    },
  ];

  let cursor: string | undefined;
  do {
    const page = await fetchCatalogSitemap(cursor);
    for (const entry of page.entries) {
      urls.push({
        url: entryUrl(entry),
        lastModified: new Date(entry.lastModified),
        changeFrequency: "weekly",
        priority: entry.type === "artist" ? 0.8 : 0.7,
      });
    }
    cursor = page.nextCursor ?? undefined;
  } while (cursor);

  return urls;
}
