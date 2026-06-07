import { ReleasePageView } from "@/components/catalog/ReleasePageView";
import { isNotFoundError } from "@/lib/api/errors";
import { getCachedCatalogReleaseBySlugs } from "@/lib/api/catalogServer";
import { getCachedCoverArtColorSeed } from "@/lib/theme/colorSeedServer";
import { releaseMetadata } from "@/lib/seo/metadata";
import { ThemeSeedStyles } from "@/theme/ThemeSeedStyles";
import type { Metadata } from "next";
import { notFound } from "next/navigation";

export const revalidate = 3600;

type ReleaseBySlugPageProps = {
  params: Promise<{ artistKey: string; releaseSlug: string }>;
};

export async function generateMetadata({ params }: ReleaseBySlugPageProps): Promise<Metadata> {
  const { artistKey, releaseSlug } = await params;
  try {
    const release = await getCachedCatalogReleaseBySlugs(artistKey, releaseSlug);
    return releaseMetadata(release);
  } catch (error) {
    if (isNotFoundError(error)) return {};
    throw error;
  }
}

export default async function ReleaseBySlugPage({ params }: ReleaseBySlugPageProps) {
  const { artistKey, releaseSlug } = await params;

  try {
    const release = await getCachedCatalogReleaseBySlugs(artistKey, releaseSlug);
    const colorSeed = release.coverArtUrl
      ? await getCachedCoverArtColorSeed(release.coverArtUrl)
      : null;
    return (
      <>
        {colorSeed ? <ThemeSeedStyles seed={colorSeed} /> : null}
        <ReleasePageView
          artistKey={artistKey}
          releaseSlug={releaseSlug}
          initialRelease={release}
          initialColorSeed={colorSeed}
        />
      </>
    );
  } catch (error) {
    if (isNotFoundError(error)) notFound();
    throw error;
  }
}
