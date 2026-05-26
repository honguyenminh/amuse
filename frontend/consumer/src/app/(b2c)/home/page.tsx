"use client";

import { AppShell } from "@/components/ui/AppShell";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Skeleton } from "@/components/ui/Skeleton";
import { Text } from "@/components/ui/Text";
import { browseCatalogHome } from "@/lib/api/catalogClient";
import type {
  AlbumSummary,
  ArtistSummary,
  BrowseHomeResponse,
} from "@/lib/api/types";
import { useAuth } from "@/lib/auth/AuthProvider";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

export default function HomePage() {
  const auth = useAuth();
  const router = useRouter();
  const [data, setData] = useState<BrowseHomeResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    browseCatalogHome()
      .then((response) => {
        if (!cancelled) setData(response);
      })
      .catch((err: Error) => {
        if (!cancelled) setError(err.message);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <AppShell
      title="Home"
      activePath="/home"
      trailing={
        <Button
          type="button"
          variant="text"
          onClick={() => void auth.logout().then(() => router.replace("/login"))}
        >
          Log out
        </Button>
      }
    >
      <div className="flex flex-col gap-6 p-4">
        <Card>
          <Text variant="headline-medium">Listener home</Text>
          <Text variant="label-medium">
            Signed in · listener id {auth.listenerId ?? "—"}
          </Text>
        </Card>

        {loading && (
          <section className="flex flex-col gap-3">
            <Skeleton className="h-6 w-40" />
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
              {Array.from({ length: 6 }, (_, i) => (
                <Card key={i}>
                  <Skeleton className="aspect-square w-full rounded-md" />
                  <Skeleton className="mt-2 h-5 w-3/4" />
                  <Skeleton className="mt-1 h-4 w-1/2" />
                </Card>
              ))}
            </div>
          </section>
        )}
        {error && (
          <Card>
            <Text variant="title-large">Could not load catalog</Text>
            <Text variant="label-medium">{error}</Text>
          </Card>
        )}

        {data && (
          <>
            <section className="flex flex-col gap-3">
              <Text variant="title-large">Recent releases</Text>
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
                {data.recentAlbums.map((album) => (
                  <AlbumTile key={album.id} album={album} />
                ))}
              </div>
            </section>

            <section className="flex flex-col gap-3">
              <Text variant="title-large">Featured artists</Text>
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
                {data.featuredArtists.map((artist) => (
                  <ArtistTile key={artist.id} artist={artist} />
                ))}
              </div>
            </section>
          </>
        )}
      </div>
    </AppShell>
  );
}

function AlbumTile({ album }: { album: AlbumSummary }) {
  return (
    <Link href={`/album/${album.id}`} className="group block">
      <Card>
        <div className="flex flex-col gap-2">
          {album.coverArtUrl && (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={album.coverArtUrl}
              alt={album.title}
              className="aspect-square w-full rounded-md object-cover"
            />
          )}
          <Text variant="title-medium">{album.title}</Text>
          <Text variant="label-medium">
            {album.artistName} · {album.releaseType}
          </Text>
        </div>
      </Card>
    </Link>
  );
}

function ArtistTile({ artist }: { artist: ArtistSummary }) {
  return (
    <Link href={`/artist/${artist.id}`} className="group block">
      <Card>
        <div className="flex flex-col gap-2">
          {artist.avatarUrl && (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={artist.avatarUrl}
              alt={artist.name}
              className="aspect-square w-full rounded-full object-cover"
            />
          )}
          <Text variant="title-medium">{artist.name}</Text>
        </div>
      </Card>
    </Link>
  );
}
