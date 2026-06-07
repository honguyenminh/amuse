import { isNotFoundError } from "@/lib/api/errors";
import { getCachedCatalogRelease } from "@/lib/api/catalogServer";
import { catalogReleasePath } from "@/lib/catalog/paths";
import { releaseMetadata } from "@/lib/seo/metadata";
import type { Metadata } from "next";
import { notFound, permanentRedirect } from "next/navigation";

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
    permanentRedirect(catalogReleasePath(release.artistSlug, release.slug));
  } catch (error) {
    if (isNotFoundError(error)) notFound();
    throw error;
  }
}
