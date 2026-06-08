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
import {
  setReleasePricing,
  setTrackPricing,
  setTrackRoyaltySplits,
  type CatalogPricingResponse,
  type ManageReleaseDetailResponse,
  type ManageTrackResponse,
  type RoyaltySplitPayeeRequest,
} from "@/lib/api/catalogClient";
import { useCallback, useEffect, useMemo, useState } from "react";

const DEFAULT_CURRENCY = "USD";

type PricingFormState = {
  isForSale: boolean;
  floorMajor: string;
  ceilingMajor: string;
  openCeiling: boolean;
  currency: string;
};

type RoyaltySplitRow = {
  payeeOrganizationId: string;
  sharePercent: string;
};

function minorToMajor(minor: number): string {
  return (minor / 100).toFixed(2);
}

export function formatPricingSummary(pricing: CatalogPricingResponse): string {
  if (!pricing.isForSale) {
    return "Not for sale";
  }

  const currency = pricing.priceCurrency ?? DEFAULT_CURRENCY;
  const floor = minorToMajor(pricing.priceFloorMinor);

  if (pricing.priceCeilingMinor === null) {
    return `PWYW from ${floor} ${currency} (open ceiling)`;
  }

  return `PWYW ${floor}–${minorToMajor(pricing.priceCeilingMinor)} ${currency}`;
}

function majorToMinor(major: string): number | null {
  const parsed = Number.parseFloat(major);
  if (!Number.isFinite(parsed) || parsed < 0) {
    return null;
  }
  return Math.round(parsed * 100);
}

function pricingToForm(pricing: CatalogPricingResponse): PricingFormState {
  const hasCeiling = pricing.priceCeilingMinor !== null;
  return {
    isForSale: pricing.isForSale,
    floorMajor: minorToMajor(pricing.priceFloorMinor),
    ceilingMajor: hasCeiling ? minorToMajor(pricing.priceCeilingMinor!) : "",
    openCeiling: pricing.isForSale && !hasCeiling,
    currency: pricing.priceCurrency ?? DEFAULT_CURRENCY,
  };
}

function splitsToRows(splits: ManageTrackResponse["royaltySplits"]): RoyaltySplitRow[] {
  if (splits.length === 0) {
    return [{ payeeOrganizationId: "", sharePercent: "" }];
  }

  return splits.map((split) => ({
    payeeOrganizationId: split.payeeOrganizationId,
    sharePercent: (split.shareBps / 100).toFixed(2),
  }));
}

function rowsToRequest(rows: RoyaltySplitRow[]): RoyaltySplitPayeeRequest[] | null {
  const filled = rows.filter(
    (row) => row.payeeOrganizationId.trim().length > 0 || row.sharePercent.trim().length > 0,
  );

  if (filled.length === 0) {
    return [];
  }

  const payload: RoyaltySplitPayeeRequest[] = [];
  let totalBps = 0;

  for (const row of filled) {
    const sharePercent = Number.parseFloat(row.sharePercent);
    if (!row.payeeOrganizationId.trim() || !Number.isFinite(sharePercent) || sharePercent <= 0) {
      return null;
    }

    const shareBps = Math.round(sharePercent * 100);
    totalBps += shareBps;
    payload.push({
      payeeOrganizationId: row.payeeOrganizationId.trim(),
      shareBps,
    });
  }

  if (totalBps !== 10_000) {
    return null;
  }

  return payload;
}

type ReleasePricingPanelProps = {
  release: ManageReleaseDetailResponse;
  canManagePricing: boolean;
  orgId: string | null;
  onReleaseUpdated: (release: ManageReleaseDetailResponse) => void;
  embedded?: boolean;
};

export function ReleasePricingPanel({
  release,
  canManagePricing,
  orgId,
  onReleaseUpdated,
  embedded = false,
}: ReleasePricingPanelProps) {
  const [releaseForm, setReleaseForm] = useState<PricingFormState>(() =>
    pricingToForm(release.pricing),
  );
  const [trackForms, setTrackForms] = useState<Record<string, PricingFormState>>(() =>
    Object.fromEntries(release.tracks.map((track) => [track.id, pricingToForm(track.pricing)])),
  );
  const [splitForms, setSplitForms] = useState<Record<string, RoyaltySplitRow[]>>(() =>
    Object.fromEntries(release.tracks.map((track) => [track.id, splitsToRows(track.royaltySplits)])),
  );
  const [savingRelease, setSavingRelease] = useState(false);
  const [savingTrackId, setSavingTrackId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setReleaseForm(pricingToForm(release.pricing));
    setTrackForms(
      Object.fromEntries(release.tracks.map((track) => [track.id, pricingToForm(track.pricing)])),
    );
    setSplitForms(
      Object.fromEntries(
        release.tracks.map((track) => [track.id, splitsToRows(track.royaltySplits)]),
      ),
    );
  }, [release]);

  const canEdit = canManagePricing && release.lifecycleStatus === "draft";

  const releaseFloorSum = useMemo(
    () =>
      release.tracks.reduce(
        (sum, track) => sum + (trackForms[track.id]?.isForSale ? track.pricing.priceFloorMinor : 0),
        0,
      ),
    [release.tracks, trackForms],
  );

  const saveReleasePricing = useCallback(async () => {
    if (!canEdit) {
      return;
    }

    const floorMinor = majorToMinor(releaseForm.floorMajor);
    if (floorMinor === null) {
      setError("Release floor price is invalid.");
      return;
    }

    let ceilingMinor: number | null = null;
    if (releaseForm.isForSale && !releaseForm.openCeiling) {
      ceilingMinor = majorToMinor(releaseForm.ceilingMajor);
      if (ceilingMinor === null) {
        setError("Release ceiling price is invalid.");
        return;
      }
    }

    setSavingRelease(true);
    setError(null);
    try {
      const updated = await setReleasePricing(release.id, {
        isForSale: releaseForm.isForSale,
        priceFloorMinor: floorMinor,
        priceCeilingMinor: releaseForm.isForSale && !releaseForm.openCeiling ? ceilingMinor : null,
        priceCurrency: releaseForm.isForSale ? releaseForm.currency.trim().toUpperCase() : null,
      });
      onReleaseUpdated(updated);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save release pricing.");
    } finally {
      setSavingRelease(false);
    }
  }, [canEdit, onReleaseUpdated, release.id, releaseForm]);

  const saveTrackPricing = useCallback(
    async (track: ManageTrackResponse) => {
      if (!canEdit) {
        return;
      }

      const form = trackForms[track.id];
      if (!form) {
        return;
      }

      const floorMinor = majorToMinor(form.floorMajor);
      if (floorMinor === null) {
        setError(`Track "${track.title}" floor price is invalid.`);
        return;
      }

      let ceilingMinor: number | null = null;
      if (form.isForSale && !form.openCeiling) {
        ceilingMinor = majorToMinor(form.ceilingMajor);
        if (ceilingMinor === null) {
          setError(`Track "${track.title}" ceiling price is invalid.`);
          return;
        }
      }

      setSavingTrackId(track.id);
      setError(null);
      try {
        const updatedTrack = await setTrackPricing(track.id, {
          isForSale: form.isForSale,
          priceFloorMinor: floorMinor,
          priceCeilingMinor: form.isForSale && !form.openCeiling ? ceilingMinor : null,
          priceCurrency: form.isForSale ? form.currency.trim().toUpperCase() : null,
        });

        onReleaseUpdated({
          ...release,
          tracks: release.tracks.map((entry) =>
            entry.id === updatedTrack.id ? updatedTrack : entry,
          ),
        });
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to save track pricing.");
      } finally {
        setSavingTrackId(null);
      }
    },
    [canEdit, onReleaseUpdated, release, trackForms],
  );

  const saveTrackSplits = useCallback(
    async (track: ManageTrackResponse) => {
      if (!canEdit) {
        return;
      }

      const rows = splitForms[track.id] ?? [];
      const payload = rowsToRequest(rows);
      if (payload === null) {
        setError(
          `Royalty splits for "${track.title}" must list payee organization IDs and shares totaling 100%.`,
        );
        return;
      }

      setSavingTrackId(track.id);
      setError(null);
      try {
        const updatedTrack = await setTrackRoyaltySplits(track.id, { splits: payload });
        onReleaseUpdated({
          ...release,
          tracks: release.tracks.map((entry) =>
            entry.id === updatedTrack.id ? updatedTrack : entry,
          ),
        });
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to save royalty splits.");
      } finally {
        setSavingTrackId(null);
      }
    },
    [canEdit, onReleaseUpdated, release, splitForms],
  );

  function updateTrackForm(trackId: string, patch: Partial<PricingFormState>) {
    setTrackForms((current) => ({
      ...current,
      [trackId]: { ...current[trackId]!, ...patch },
    }));
  }

  function updateSplitRow(trackId: string, index: number, patch: Partial<RoyaltySplitRow>) {
    setSplitForms((current) => {
      const rows = [...(current[trackId] ?? [])];
      rows[index] = { ...rows[index]!, ...patch };
      return { ...current, [trackId]: rows };
    });
  }

  function addSplitRow(trackId: string) {
    setSplitForms((current) => ({
      ...current,
      [trackId]: [...(current[trackId] ?? []), { payeeOrganizationId: "", sharePercent: "" }],
    }));
  }

  function removeSplitRow(trackId: string, index: number) {
    setSplitForms((current) => {
      const rows = [...(current[trackId] ?? [])];
      rows.splice(index, 1);
      return {
        ...current,
        [trackId]: rows.length > 0 ? rows : [{ payeeOrganizationId: "", sharePercent: "" }],
      };
    });
  }

  if (!canManagePricing) {
    return null;
  }

  const description = (
    <>
      Pay what you want (PWYW) pricing and per-track royalty splits. Release floor must be at most
      the sum of track floors ({minorToMajor(releaseFloorSum)} {releaseForm.currency} with current
      saved track values).
    </>
  );

  const content = (
    <>
        {error ? <p className="text-sm text-destructive">{error}</p> : null}

        <section className="space-y-3 rounded-lg border p-4">
          <h3 className="text-sm font-medium">Release bundle</h3>
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={releaseForm.isForSale}
              disabled={!canEdit}
              onChange={(event) =>
                setReleaseForm((current) => ({ ...current, isForSale: event.target.checked }))
              }
            />
            Sell this release as a bundle
          </label>
          {releaseForm.isForSale ? (
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="space-y-1">
                <Label htmlFor="release-floor">Minimum price</Label>
                <Input
                  id="release-floor"
                  type="number"
                  min={0}
                  step="0.01"
                  disabled={!canEdit}
                  value={releaseForm.floorMajor}
                  onChange={(event) =>
                    setReleaseForm((current) => ({ ...current, floorMajor: event.target.value }))
                  }
                />
              </div>
              <div className="space-y-1">
                <Label htmlFor="release-currency">Currency</Label>
                <Input
                  id="release-currency"
                  maxLength={3}
                  disabled={!canEdit}
                  value={releaseForm.currency}
                  onChange={(event) =>
                    setReleaseForm((current) => ({ ...current, currency: event.target.value }))
                  }
                />
              </div>
              <label className="flex items-center gap-2 text-sm sm:col-span-2">
                <input
                  type="checkbox"
                  checked={releaseForm.openCeiling}
                  disabled={!canEdit}
                  onChange={(event) =>
                    setReleaseForm((current) => ({ ...current, openCeiling: event.target.checked }))
                  }
                />
                Open ceiling (buyer may pay more than the minimum)
              </label>
              {!releaseForm.openCeiling ? (
                <div className="space-y-1 sm:col-span-2">
                  <Label htmlFor="release-ceiling">Maximum price</Label>
                  <Input
                    id="release-ceiling"
                    type="number"
                    min={0}
                    step="0.01"
                    disabled={!canEdit}
                    value={releaseForm.ceilingMajor}
                    onChange={(event) =>
                      setReleaseForm((current) => ({
                        ...current,
                        ceilingMajor: event.target.value,
                      }))
                    }
                  />
                </div>
              ) : null}
            </div>
          ) : null}
          {canEdit ? (
            <Button
              type="button"
              size="sm"
              disabled={savingRelease}
              onClick={() => void saveReleasePricing()}
            >
              {savingRelease ? "Saving release pricing…" : "Save release pricing"}
            </Button>
          ) : null}
        </section>

        {release.tracks.map((track) => {
          const form = trackForms[track.id] ?? pricingToForm(track.pricing);
          const rows = splitForms[track.id] ?? splitsToRows(track.royaltySplits);
          const busy = savingTrackId === track.id;

          return (
            <section key={track.id} className="space-y-3 rounded-lg border p-4">
              <h3 className="text-sm font-medium">
                Track {track.trackNumber}: {track.title}
              </h3>
              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={form.isForSale}
                  disabled={!canEdit}
                  onChange={(event) =>
                    updateTrackForm(track.id, { isForSale: event.target.checked })
                  }
                />
                Sell this track individually
              </label>
              {form.isForSale ? (
                <div className="grid gap-3 sm:grid-cols-2">
                  <div className="space-y-1">
                    <Label>Minimum price</Label>
                    <Input
                      type="number"
                      min={0}
                      step="0.01"
                      disabled={!canEdit}
                      value={form.floorMajor}
                      onChange={(event) =>
                        updateTrackForm(track.id, { floorMajor: event.target.value })
                      }
                    />
                  </div>
                  <div className="space-y-1">
                    <Label>Currency</Label>
                    <Input
                      maxLength={3}
                      disabled={!canEdit}
                      value={form.currency}
                      onChange={(event) =>
                        updateTrackForm(track.id, { currency: event.target.value })
                      }
                    />
                  </div>
                  <label className="flex items-center gap-2 text-sm sm:col-span-2">
                    <input
                      type="checkbox"
                      checked={form.openCeiling}
                      disabled={!canEdit}
                      onChange={(event) =>
                        updateTrackForm(track.id, { openCeiling: event.target.checked })
                      }
                    />
                    Open ceiling
                  </label>
                  {!form.openCeiling ? (
                    <div className="space-y-1 sm:col-span-2">
                      <Label>Maximum price</Label>
                      <Input
                        type="number"
                        min={0}
                        step="0.01"
                        disabled={!canEdit}
                        value={form.ceilingMajor}
                        onChange={(event) =>
                          updateTrackForm(track.id, { ceilingMajor: event.target.value })
                        }
                      />
                    </div>
                  ) : null}
                </div>
              ) : null}

              <div className="space-y-2">
                <p className="text-sm text-muted-foreground">
                  Royalty splits (organization payees only). Leave empty for 100% to the listing
                  org{orgId ? ` (${orgId})` : ""}.
                </p>
                {rows.map((row, index) => (
                  <div key={`${track.id}-split-${index}`} className="grid gap-2 sm:grid-cols-[1fr_120px_auto]">
                    <Input
                      placeholder="Payee organization ID"
                      disabled={!canEdit}
                      value={row.payeeOrganizationId}
                      onChange={(event) =>
                        updateSplitRow(track.id, index, {
                          payeeOrganizationId: event.target.value,
                        })
                      }
                    />
                    <Input
                      type="number"
                      min={0}
                      max={100}
                      step="0.01"
                      placeholder="% share"
                      disabled={!canEdit}
                      value={row.sharePercent}
                      onChange={(event) =>
                        updateSplitRow(track.id, index, { sharePercent: event.target.value })
                      }
                    />
                    {canEdit && rows.length > 1 ? (
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() => removeSplitRow(track.id, index)}
                      >
                        Remove
                      </Button>
                    ) : null}
                  </div>
                ))}
                {canEdit ? (
                  <div className="flex flex-wrap gap-2">
                    <Button type="button" variant="outline" size="sm" onClick={() => addSplitRow(track.id)}>
                      Add payee
                    </Button>
                    <Button
                      type="button"
                      size="sm"
                      disabled={busy}
                      onClick={() => void saveTrackPricing(track)}
                    >
                      {busy ? "Saving…" : "Save track pricing"}
                    </Button>
                    <Button
                      type="button"
                      size="sm"
                      variant="secondary"
                      disabled={busy}
                      onClick={() => void saveTrackSplits(track)}
                    >
                      {busy ? "Saving…" : "Save royalty splits"}
                    </Button>
                  </div>
                ) : null}
              </div>
            </section>
          );
        })}
    </>
  );

  if (embedded) {
    return (
      <div className="space-y-6">
        <p className="text-sm text-muted-foreground">{description}</p>
        {content}
      </div>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Sales & pricing</CardTitle>
        <CardDescription>{description}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-6">{content}</CardContent>
    </Card>
  );
}
