import { AppShell } from "@/components/ui/AppShell";
import { PageContent } from "@/components/ui/PageContent";
import { Skeleton } from "@/components/ui/Skeleton";
import { Suspense } from "react";
import { SearchPageClient } from "./SearchPageClient";

function SearchPageFallback() {
  return (
    <AppShell title="Search" activePath="/search">
      <PageContent gap="6">
        <Skeleton className="h-10 w-full max-w-xl rounded-full" />
        <Skeleton className="h-32 w-full" />
      </PageContent>
    </AppShell>
  );
}

export default function SearchPage() {
  return (
    <Suspense fallback={<SearchPageFallback />}>
      <SearchPageClient />
    </Suspense>
  );
}
