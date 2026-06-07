import { Skeleton } from "@/components/ui/Skeleton";
import { Suspense } from "react";
import { SearchPageClient } from "./SearchPageClient";

export default function SearchPage() {
  return (
    <Suspense
      fallback={
        <div className="flex flex-col gap-3 p-6">
          <Skeleton className="h-10 w-full max-w-xl" />
          <Skeleton className="h-32 w-full" />
        </div>
      }
    >
      <SearchPageClient />
    </Suspense>
  );
}
