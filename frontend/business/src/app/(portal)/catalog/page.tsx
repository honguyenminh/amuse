"use client";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { useAuth } from "@/lib/auth/AuthProvider";
import {
  listArtists,
  type ManageArtistSummaryResponse,
} from "@/lib/api/catalogClient";
import { hasClaim } from "@/lib/auth/jwtClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import { Plus } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

export default function CatalogPage() {
  const auth = useAuth();
  const orgId = auth.activePersona?.type === "org" ? auth.activePersona.orgId : null;
  const token = getAccessToken();
  const canRead = hasClaim(token, "read:catalog:all");
  const canWrite = hasClaim(token, "write_draft:catalog:all");

  const [artists, setArtists] = useState<ManageArtistSummaryResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!orgId || !canRead) {
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(null);

    listArtists()
      .then((response) => {
        if (!cancelled) {
          setArtists(response.items);
        }
      })
      .catch((err) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load artists.");
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [orgId, canRead]);

  if (!orgId) {
    return (
      <p className="text-sm text-muted-foreground">
        Select an organization workspace to manage catalog content.
      </p>
    );
  }

  if (!canRead) {
    return (
      <p className="text-sm text-muted-foreground">
        Your current workspace token does not include catalog read permission.
      </p>
    );
  }

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Catalog</h1>
          <p className="text-sm text-muted-foreground">
            Manage artists and releases for your organization.
          </p>
        </div>
        <div className="flex gap-2">
          {canWrite ? (
            <Button render={<Link href="/catalog/artists/new" />}>
              <Plus />
              Add artist
            </Button>
          ) : null}
        </div>
      </div>

      {error ? <p className="text-sm text-destructive">{error}</p> : null}

      <Card>
        <CardHeader>
          <CardTitle>Artist roster</CardTitle>
          <CardDescription>
            {loading
              ? "Loading artists…"
              : `${artists.length} artist${artists.length === 1 ? "" : "s"}`}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {artists.length === 0 && !loading ? (
            <p className="text-sm text-muted-foreground">
              No artists yet.
              {canWrite ? " Create your first artist to start uploading releases." : ""}
            </p>
          ) : (
            <ul className="divide-y">
              {artists.map((artist) => (
                <li key={artist.id}>
                  <Link
                    href={`/catalog/artists/${artist.id}`}
                    className="flex items-center justify-between gap-4 py-3 transition-colors hover:text-primary"
                  >
                    <div>
                      <p className="font-medium">{artist.name}</p>
                      <p className="text-xs text-muted-foreground">/{artist.slug}</p>
                    </div>
                    <span className="text-xs text-muted-foreground">
                      Added {formatDate(artist.createdAt)}
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
