import { ReleaseGroupPageClient } from "@/components/catalog/ReleaseGroupPageClient";
import { isNotFoundError } from "@/lib/api/errors";
import { getCachedCatalogReleaseGroupBySlugs } from "@/lib/api/catalogServer";
import { getCachedCoverArtColorSeed } from "@/lib/theme/colorSeedServer";
import { releaseGroupMetadata } from "@/lib/seo/metadata";
import { ThemeSeedStyles } from "@/theme/ThemeSeedStyles";
import type { Metadata } from "next";
import { notFound } from "next/navigation";

export const revalidate = 3600;

type ReleaseGroupPageProps = {
  params: Promise<{ artistKey: string; groupSlug: string }>;
};

export async function generateMetadata({ params }: ReleaseGroupPageProps): Promise<Metadata> {
  const { artistKey, groupSlug } = await params;
  try {
    const group = await getCachedCatalogReleaseGroupBySlugs(artistKey, groupSlug);
    return releaseGroupMetadata(group);
  } catch (error) {
    if (isNotFoundError(error)) return {};
    throw error;
  }
}

export default async function ReleaseGroupPage({ params }: ReleaseGroupPageProps) {
  const { artistKey, groupSlug } = await params;

  try {
    const group = await getCachedCatalogReleaseGroupBySlugs(artistKey, groupSlug);
    const coverUrl = group.releases.find((edition) => edition.coverArtUrl)?.coverArtUrl ?? null;
    const colorSeed = coverUrl ? await getCachedCoverArtColorSeed(coverUrl) : null;
    return (
      <>
        {colorSeed ? <ThemeSeedStyles seed={colorSeed} /> : null}
        <ReleaseGroupPageClient
          artistKey={artistKey}
          groupSlug={groupSlug}
          initialGroup={group}
          initialColorSeed={colorSeed}
        />
      </>
    );
  } catch (error) {
    if (isNotFoundError(error)) notFound();
    throw error;
  }
}
