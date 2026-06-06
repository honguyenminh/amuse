"use client";

import { AppShell } from "@/components/ui/AppShell";
import { Card } from "@/components/ui/Card";
import { Text } from "@/components/ui/Text";
import { isValidHashtagTag, normalizeHashtagTag } from "@amuse/catalog-text";
import { use } from "react";

export default function HashtagPage({
  params,
}: {
  params: Promise<{ tag: string }>;
}) {
  const { tag: rawTag } = use(params);
  const decodedTag = decodeURIComponent(rawTag);
  const normalizedTag = normalizeHashtagTag(decodedTag);
  const valid = normalizedTag !== null && isValidHashtagTag(decodedTag);

  return (
    <AppShell title={valid ? `#${normalizedTag}` : "Hashtag"} activePath="/home">
      <div className="flex flex-col gap-4 p-4">
        <Card>
          {valid ? (
            <>
              <Text variant="title-large">#{normalizedTag}</Text>
              <Text variant="body-medium" className="mt-2 text-on-surface-variant">
                Hashtag details are coming soon. Check back later for releases and artists tagged
                with #{normalizedTag}.
              </Text>
            </>
          ) : (
            <>
              <Text variant="title-large">Invalid hashtag</Text>
              <Text variant="body-medium" className="mt-2 text-on-surface-variant">
                This hashtag link is not valid.
              </Text>
            </>
          )}
        </Card>
      </div>
    </AppShell>
  );
}
