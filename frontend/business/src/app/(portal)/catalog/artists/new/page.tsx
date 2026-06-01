"use client";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { checkArtistSlugAvailability, createArtist } from "@/lib/api/catalogClient";
import { ApiError } from "@/lib/api/types";
import {
  isValidArtistSlug,
  normalizeSlugInput,
  slugValidationMessage,
  suggestArtistSlugFromName,
} from "@/lib/catalog/slug";
import { useAuth } from "@/lib/auth/AuthProvider";
import { hasClaim } from "@/lib/auth/jwtClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

type SlugStatus = "idle" | "checking" | "available" | "taken" | "invalid";

export default function NewArtistPage() {
  const auth = useAuth();
  const router = useRouter();
  const orgId = auth.activePersona?.type === "org" ? auth.activePersona.orgId : null;
  const token = getAccessToken();
  const canWrite = hasClaim(token, "write_draft:catalog:all");

  const [name, setName] = useState("");
  const [slug, setSlug] = useState("");
  const [slugManuallyEdited, setSlugManuallyEdited] = useState(false);
  const [slugStatus, setSlugStatus] = useState<SlugStatus>("idle");
  const [bio, setBio] = useState("");
  const [countryCode, setCountryCode] = useState("");
  const [websiteUrl, setWebsiteUrl] = useState("");
  const [aliases, setAliases] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (slugManuallyEdited) {
      return;
    }
    setSlug(suggestArtistSlugFromName(name));
  }, [name, slugManuallyEdited]);

  useEffect(() => {
    const normalized = normalizeSlugInput(slug);
    const validationError = slugValidationMessage(slug);
    if (!normalized || validationError) {
      setSlugStatus(normalized ? "invalid" : "idle");
      return;
    }

    let cancelled = false;
    setSlugStatus("checking");

    const timeout = window.setTimeout(() => {
      void checkArtistSlugAvailability(normalized)
        .then((response) => {
          if (cancelled) {
            return;
          }
          if (!response.isValid) {
            setSlugStatus("invalid");
            return;
          }
          setSlugStatus(response.isAvailable ? "available" : "taken");
        })
        .catch(() => {
          if (!cancelled) {
            setSlugStatus("idle");
          }
        });
    }, 300);

    return () => {
      cancelled = true;
      window.clearTimeout(timeout);
    };
  }, [slug]);

  const normalizedSlug = normalizeSlugInput(slug);
  const slugValidationError = slug ? slugValidationMessage(slug) : null;
  const canSubmit =
    name.trim().length > 0 &&
    isValidArtistSlug(normalizedSlug) &&
    slugStatus === "available" &&
    !submitting;

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    const trimmedName = name.trim();
    const trimmedSlug = normalizeSlugInput(slug);
    if (!trimmedName) {
      setError("Artist name is required.");
      return;
    }
    if (!isValidArtistSlug(trimmedSlug)) {
      setError(slugValidationMessage(slug) ?? "Slug is invalid.");
      return;
    }
    if (slugStatus !== "available") {
      setError("Choose an available slug before creating the artist.");
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      const artist = await createArtist({
        name: trimmedName,
        slug: trimmedSlug,
        bio: bio.trim() || null,
        countryCode: countryCode.trim() || null,
        websiteUrl: websiteUrl.trim() || null,
        aliases: aliases.trim() || null,
      });
      router.push(`/catalog/artists/${artist.id}`);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
        if (err.code === "catalog.duplicate_slug") {
          setSlugStatus("taken");
        }
      } else {
        setError(err instanceof Error ? err.message : "Could not create artist.");
      }
    } finally {
      setSubmitting(false);
    }
  }

  if (!orgId) {
    return (
      <p className="text-sm text-muted-foreground">
        Select an organization workspace to create an artist.
      </p>
    );
  }

  if (!canWrite) {
    return (
      <p className="text-sm text-muted-foreground">
        Your current workspace token does not include catalog write permission.
      </p>
    );
  }

  return (
    <div className="mx-auto flex w-full max-w-lg flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">New artist</h1>
        <p className="text-sm text-muted-foreground">
          Add an artist to your organization roster.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Artist profile</CardTitle>
          <CardDescription>
            The URL slug is suggested from the name but you can edit it before saving.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form className="flex flex-col gap-4" onSubmit={onSubmit}>
            <div className="grid gap-2">
              <Label htmlFor="name">Name</Label>
              <Input
                id="name"
                value={name}
                onChange={(event) => setName(event.target.value)}
                placeholder="Artist or band name"
                required
                disabled={submitting}
              />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="slug">URL slug</Label>
              <Input
                id="slug"
                value={slug}
                onChange={(event) => {
                  setSlugManuallyEdited(true);
                  setSlug(event.target.value);
                }}
                placeholder="artist-slug"
                required
                disabled={submitting}
                autoComplete="off"
                spellCheck={false}
              />
              <p className="text-xs text-muted-foreground">
                Public path: /artists/{normalizedSlug || "your-slug"}
              </p>
              {slugValidationError ? (
                <p className="text-xs text-destructive">{slugValidationError}</p>
              ) : slugStatus === "checking" ? (
                <p className="text-xs text-muted-foreground">Checking availability…</p>
              ) : slugStatus === "available" ? (
                <p className="text-xs text-green-600 dark:text-green-400">Slug is available.</p>
              ) : slugStatus === "taken" ? (
                <p className="text-xs text-destructive">This slug is already taken.</p>
              ) : null}
            </div>

            <div className="grid gap-2">
              <Label htmlFor="bio">Bio</Label>
              <textarea
                id="bio"
                value={bio}
                onChange={(event) => setBio(event.target.value)}
                placeholder="Optional short biography"
                rows={4}
                disabled={submitting}
                className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              />
            </div>

            <div className="grid gap-2 sm:grid-cols-2">
              <div className="grid gap-2">
                <Label htmlFor="countryCode">Country code</Label>
                <Input
                  id="countryCode"
                  value={countryCode}
                  onChange={(event) => setCountryCode(event.target.value)}
                  placeholder="e.g. US"
                  disabled={submitting}
                />
              </div>
              <div className="grid gap-2">
                <Label htmlFor="websiteUrl">Website</Label>
                <Input
                  id="websiteUrl"
                  value={websiteUrl}
                  onChange={(event) => setWebsiteUrl(event.target.value)}
                  placeholder="https://example.com"
                  disabled={submitting}
                />
              </div>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="aliases">Aliases</Label>
              <Input
                id="aliases"
                value={aliases}
                onChange={(event) => setAliases(event.target.value)}
                placeholder="Comma-separated aliases"
                disabled={submitting}
              />
            </div>

            {error ? <p className="text-sm text-destructive">{error}</p> : null}

            <div className="flex flex-wrap gap-2">
              <Button type="submit" disabled={!canSubmit}>
                {submitting ? "Creating…" : "Create artist"}
              </Button>
              <Button
                type="button"
                variant="outline"
                disabled={submitting}
                render={<Link href="/catalog" />}
              >
                Cancel
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
