"use client";

import { SearchResults } from "@/components/discovery/SearchResults";
import { AppShell } from "@/components/ui/AppShell";
import { Card } from "@/components/ui/Card";
import { PageContent } from "@/components/ui/PageContent";
import { Skeleton } from "@/components/ui/Skeleton";
import { Text } from "@/components/ui/Text";
import { searchDiscovery } from "@/lib/api/discoveryClient";
import type { SearchResponse } from "@/lib/api/types";
import { searchPath } from "@/lib/discovery/paths";
import { useRouter, useSearchParams } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";

export default function SearchPage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const query = searchParams.get("q")?.trim() ?? "";

  const [input, setInput] = useState(query);
  const [data, setData] = useState<SearchResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    setInput(query);
  }, [query]);

  useEffect(() => {
    if (!query) {
      setData(null);
      setError(null);
      setLoading(false);
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(null);
    searchDiscovery(query)
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
  }, [query]);

  const onSubmit = (event: FormEvent) => {
    event.preventDefault();
    router.push(searchPath(input));
  };

  return (
    <AppShell title="Search" activePath="/search">
      <PageContent gap="6">
        <form onSubmit={onSubmit} className="flex gap-2">
          <input
            type="search"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            placeholder="Search artists, releases, tracks, playlists"
            className="min-w-0 flex-1 rounded-full border-2 border-outline bg-background px-4 py-2 text-body-medium"
            aria-label="Search query"
          />
          <button
            type="submit"
            className="rounded-full border-2 border-outline bg-primary px-4 py-2 text-label-large text-on-primary"
          >
            Search
          </button>
        </form>

        {!query ? (
          <Card>
            <Text variant="title-large">Search Amuse</Text>
            <Text variant="label-medium" className="text-on-surface-variant">
              Find artists, releases, tracks, and public playlists.
            </Text>
          </Card>
        ) : null}

        {loading ? (
          <div className="flex flex-col gap-3">
            <Skeleton className="h-7 w-40" />
            <Skeleton className="h-32 w-full" />
            <Skeleton className="h-32 w-full" />
          </div>
        ) : null}

        {error ? (
          <Card>
            <Text variant="title-large">Search failed</Text>
            <Text variant="label-medium">{error}</Text>
          </Card>
        ) : null}

        {data && query ? <SearchResults data={data} query={query} /> : null}
      </PageContent>
    </AppShell>
  );
}
