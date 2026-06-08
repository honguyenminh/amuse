import { ArtistPageClient } from "@/components/catalog/ArtistPageClient";
import { isNotFoundError } from "@/lib/api/errors";
import { getCachedCatalogArtist } from "@/lib/api/catalogServer";
import { getCachedCoverArtColorSeed } from "@/lib/theme/colorSeedServer";
import { artistMetadata } from "@/lib/seo/metadata";
import { ThemeSeedStyles } from "@/theme/ThemeSeedStyles";
import type { Metadata } from "next";
import { notFound } from "next/navigation";

export const revalidate = 3600;

type ArtistPageProps = {
  params: Promise<{ artistKey: string }>;
};

export async function generateMetadata({ params }: ArtistPageProps): Promise<Metadata> {
  const { artistKey } = await params;
  try {
    const artist = await getCachedCatalogArtist(artistKey);
    return artistMetadata(artist);
  } catch (error) {
    if (isNotFoundError(error)) return {};
    throw error;
  }
}

export default async function ArtistPage({ params }: ArtistPageProps) {
  const { artistKey } = await params;

  let artist;
  let colorSeed = null;
  try {
    artist = await getCachedCatalogArtist(artistKey);
    const coverUrl = artist.coverUrl ?? artist.avatarUrl;
    colorSeed = coverUrl ? await getCachedCoverArtColorSeed(coverUrl) : null;
  } catch (error) {
    if (isNotFoundError(error)) notFound();
    throw error;
  }

  return (
    <>
      {colorSeed ? <ThemeSeedStyles seed={colorSeed} /> : null}
      <ArtistPageClient
        key={artistKey}
        artistKey={artistKey}
        initialArtist={artist}
        initialColorSeed={colorSeed}
      />
    </>
  );
}
