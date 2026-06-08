import { ReleasePageView } from "@/components/catalog/ReleasePageView";
import { isNotFoundError } from "@/lib/api/errors";
import { getCachedCatalogRelease } from "@/lib/api/catalogServer";
import { getCachedCoverArtColorSeed } from "@/lib/theme/colorSeedServer";
import { releaseMetadata } from "@/lib/seo/metadata";
import { ThemeSeedStyles } from "@/theme/ThemeSeedStyles";
import type { Metadata } from "next";
import { notFound } from "next/navigation";

export const revalidate = 3600;

type ReleaseByIdPageProps = {
  params: Promise<{ releaseId: string }>;
};

export async function generateMetadata({ params }: ReleaseByIdPageProps): Promise<Metadata> {
  const { releaseId } = await params;
  try {
    const release = await getCachedCatalogRelease(releaseId);
    return releaseMetadata(release);
  } catch (error) {
    if (isNotFoundError(error)) return {};
    throw error;
  }
}

export default async function ReleaseByIdPage({ params }: ReleaseByIdPageProps) {
  const { releaseId } = await params;

  try {
    const release = await getCachedCatalogRelease(releaseId);
    const colorSeed = release.coverArtUrl
      ? await getCachedCoverArtColorSeed(release.coverArtUrl)
      : null;
    return (
      <>
        {colorSeed ? <ThemeSeedStyles seed={colorSeed} /> : null}
        <ReleasePageView
          releaseId={releaseId}
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
