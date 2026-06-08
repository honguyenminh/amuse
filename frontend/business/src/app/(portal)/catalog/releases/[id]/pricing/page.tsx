"use client";

import {
  formatPricingSummary,
  ReleasePricingPanel,
} from "@/components/catalog/ReleasePricingPanel";
import { getRelease, type ManageReleaseDetailResponse } from "@/lib/api/catalogClient";
import { useAuth } from "@/lib/auth/AuthProvider";
import { hasClaim } from "@/lib/auth/jwtClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";

export default function ReleasePricingPage() {
  const params = useParams<{ id: string }>();
  const releaseId = params.id;
  const auth = useAuth();
  const orgId = auth.activePersona?.type === "org" ? auth.activePersona.orgId : null;
  const token = getAccessToken();
  const canRead = hasClaim(token, "read:catalog:all");
  const canManagePricing = hasClaim(token, "manage:catalog:pricing:all");

  const [release, setRelease] = useState<ManageReleaseDetailResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadRelease = useCallback(async () => {
    if (!releaseId) {
      return;
    }
    const data = await getRelease(releaseId);
    setRelease(data);
  }, [releaseId]);

  useEffect(() => {
    if (!orgId || !canRead || !releaseId) {
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(null);

    loadRelease()
      .catch((err) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load release.");
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
  }, [orgId, canRead, releaseId, loadRelease]);

  if (!orgId) {
    return (
      <p className="text-sm text-muted-foreground">
        Select an organization workspace to manage release pricing.
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

  if (!canManagePricing) {
    return (
      <p className="text-sm text-muted-foreground">
        Your current workspace token does not include catalog pricing permission.
      </p>
    );
  }

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-4">
      <div>
        {release ? (
          <Link
            href={`/catalog/releases/${release.id}`}
            className="text-sm text-muted-foreground hover:text-foreground"
          >
            ← {release.title}
          </Link>
        ) : null}
        <h1 className="mt-1 text-2xl font-semibold tracking-tight">
          Sales & pricing
        </h1>
        {release ? (
          <p className="text-sm text-muted-foreground">
            {formatPricingSummary(release.pricing)}
            {release.lifecycleStatus !== "draft"
              ? " · Pricing is locked after publish."
              : ""}
          </p>
        ) : (
          <p className="text-sm text-muted-foreground">
            {loading ? "Loading release…" : "Pay what you want pricing and royalty splits."}
          </p>
        )}
      </div>

      {error ? <p className="text-sm text-destructive">{error}</p> : null}

      {release ? (
        <ReleasePricingPanel
          embedded
          release={release}
          canManagePricing={canManagePricing}
          orgId={orgId}
          onReleaseUpdated={setRelease}
        />
      ) : null}
    </div>
  );
}
