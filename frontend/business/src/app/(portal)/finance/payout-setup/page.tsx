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
import { hasClaim } from "@/lib/auth/jwtClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import {
  getPayoutProfile,
  submitPayoutProfile,
  upsertPayoutProfile,
  type LegalEntityType,
  type PayoutProfileResponse,
  type PayoutRail,
} from "@/lib/api/financeClient";
import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";

type WizardStep = "entity" | "address" | "payout" | "documents" | "review";

const steps: WizardStep[] = ["entity", "address", "payout", "documents", "review"];

export default function PayoutSetupPage() {
  const token = getAccessToken();
  const canManage = hasClaim(token, "manage:payout:profile:all");
  const canRead = hasClaim(token, "read:payout:all");

  const [step, setStep] = useState<WizardStep>("entity");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [profile, setProfile] = useState<PayoutProfileResponse | null>(null);

  const [legalEntityType, setLegalEntityType] = useState<LegalEntityType>("individual");
  const [legalName, setLegalName] = useState("");
  const [representativeName, setRepresentativeName] = useState("");
  const [addressLine1, setAddressLine1] = useState("");
  const [addressLine2, setAddressLine2] = useState("");
  const [city, setCity] = useState("");
  const [region, setRegion] = useState("");
  const [postalCode, setPostalCode] = useState("");
  const [countryCode, setCountryCode] = useState("VN");
  const [taxId, setTaxId] = useState("");
  const [payoutRail, setPayoutRail] = useState<PayoutRail>("manualBank");
  const [bankAccountNumber, setBankAccountNumber] = useState("");
  const [bankName, setBankName] = useState("");
  const [documentKeysText, setDocumentKeysText] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const existing = await getPayoutProfile();
      setProfile(existing);
      if (existing) {
        setLegalEntityType(existing.legalEntityType);
        setLegalName(existing.legalName);
        setRepresentativeName(existing.representativeName ?? "");
        setAddressLine1(existing.addressLine1);
        setAddressLine2(existing.addressLine2 ?? "");
        setCity(existing.city);
        setRegion(existing.region ?? "");
        setPostalCode(existing.postalCode);
        setCountryCode(existing.countryCode);
        setPayoutRail(existing.payoutRail);
        setBankName(existing.bankName ?? "");
        setDocumentKeysText(existing.documentObjectKeys.join("\n"));
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load payout profile.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (canRead) {
      void load();
    }
  }, [canRead, load]);

  const stepIndex = steps.indexOf(step);
  const documentObjectKeys = useMemo(
    () =>
      documentKeysText
        .split("\n")
        .map((line) => line.trim())
        .filter(Boolean),
    [documentKeysText],
  );

  async function saveDraft() {
    if (!canManage) {
      return;
    }
    setSaving(true);
    setError(null);
    try {
      const saved = await upsertPayoutProfile({
        legalEntityType,
        legalName,
        addressLine1,
        addressLine2: addressLine2 || null,
        city,
        region: region || null,
        postalCode,
        countryCode,
        taxId: taxId || null,
        representativeName:
          legalEntityType === "company" ? representativeName || null : null,
        payoutRail,
        bankAccountNumber: bankAccountNumber || null,
        bankName: bankName || null,
        documentObjectKeys,
      });
      setProfile(saved);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not save payout profile.");
    } finally {
      setSaving(false);
    }
  }

  async function onSubmitForReview() {
    if (!canManage) {
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      await saveDraft();
      const submitted = await submitPayoutProfile();
      setProfile(submitted);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Submission failed.");
    } finally {
      setSubmitting(false);
    }
  }

  if (!canRead) {
    return (
      <p className="text-sm text-muted-foreground">
        Your organization token does not include payout read access.
      </p>
    );
  }

  const statusLabel = profile?.verificationStatus ?? "notStarted";

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-6">
      <Card>
        <CardHeader>
          <CardTitle>Payout setup (Gate B)</CardTitle>
          <CardDescription>
            Complete KYC and bank details so your organization can withdraw seller
            earnings. Earnings accrue before Gate B is verified.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-2 text-sm">
          <p>
            Status:{" "}
            <span className="font-medium text-foreground">{statusLabel}</span>
          </p>
          {profile?.rejectionReason ? (
            <p className="text-destructive">Rejected: {profile.rejectionReason}</p>
          ) : null}
          {profile?.verificationStatus === "verified" ? (
            <p className="text-muted-foreground">
              Your payout profile is verified. Material changes will require
              platform re-review and temporarily block withdrawals.
            </p>
          ) : null}
        </CardContent>
      </Card>

      {loading ? <p className="text-sm text-muted-foreground">Loading…</p> : null}
      {error ? <p className="text-sm text-destructive">{error}</p> : null}

      {!loading && canManage && profile?.verificationStatus !== "underReview" ? (
        <>
          <div className="flex flex-wrap gap-2">
            {steps.map((item, index) => (
              <Button
                key={item}
                size="sm"
                variant={item === step ? "default" : "outline"}
                onClick={() => setStep(item)}
              >
                {index + 1}. {item}
              </Button>
            ))}
          </div>

          <Card>
            <CardContent className="space-y-4 pt-6">
              {step === "entity" ? (
                <>
                  <div className="space-y-2">
                    <Label htmlFor="legalEntityType">Legal entity type</Label>
                    <select
                      id="legalEntityType"
                      className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs"
                      value={legalEntityType}
                      onChange={(event) =>
                        setLegalEntityType(event.target.value as LegalEntityType)
                      }
                    >
                      <option value="individual">Individual</option>
                      <option value="company">Company</option>
                    </select>
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="legalName">Legal name</Label>
                    <Input
                      id="legalName"
                      value={legalName}
                      onChange={(event) => setLegalName(event.target.value)}
                    />
                  </div>
                  {legalEntityType === "company" ? (
                    <div className="space-y-2">
                      <Label htmlFor="representativeName">Authorized representative</Label>
                      <Input
                        id="representativeName"
                        value={representativeName}
                        onChange={(event) => setRepresentativeName(event.target.value)}
                      />
                    </div>
                  ) : null}
                </>
              ) : null}

              {step === "address" ? (
                <>
                  <div className="space-y-2">
                    <Label htmlFor="addressLine1">Address line 1</Label>
                    <Input
                      id="addressLine1"
                      value={addressLine1}
                      onChange={(event) => setAddressLine1(event.target.value)}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="addressLine2">Address line 2</Label>
                    <Input
                      id="addressLine2"
                      value={addressLine2}
                      onChange={(event) => setAddressLine2(event.target.value)}
                    />
                  </div>
                  <div className="grid gap-4 sm:grid-cols-2">
                    <div className="space-y-2">
                      <Label htmlFor="city">City</Label>
                      <Input
                        id="city"
                        value={city}
                        onChange={(event) => setCity(event.target.value)}
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="region">Region / state</Label>
                      <Input
                        id="region"
                        value={region}
                        onChange={(event) => setRegion(event.target.value)}
                      />
                    </div>
                  </div>
                  <div className="grid gap-4 sm:grid-cols-2">
                    <div className="space-y-2">
                      <Label htmlFor="postalCode">Postal code</Label>
                      <Input
                        id="postalCode"
                        value={postalCode}
                        onChange={(event) => setPostalCode(event.target.value)}
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="countryCode">Country (ISO)</Label>
                      <Input
                        id="countryCode"
                        value={countryCode}
                        maxLength={2}
                        onChange={(event) =>
                          setCountryCode(event.target.value.toUpperCase())
                        }
                      />
                    </div>
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="taxId">Tax ID</Label>
                    <Input
                      id="taxId"
                      type="password"
                      autoComplete="off"
                      placeholder={profile?.hasTaxId ? "Saved — enter to replace" : ""}
                      value={taxId}
                      onChange={(event) => setTaxId(event.target.value)}
                    />
                  </div>
                </>
              ) : null}

              {step === "payout" ? (
                <>
                  <div className="space-y-2">
                    <Label htmlFor="payoutRail">Payout method</Label>
                    <select
                      id="payoutRail"
                      className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs"
                      value={payoutRail}
                      onChange={(event) =>
                        setPayoutRail(event.target.value as PayoutRail)
                      }
                    >
                      <option value="manualBank">Manual bank transfer (DA1)</option>
                      <option value="stripeGlobal">
                        Stripe Global Payouts (coming soon)
                      </option>
                    </select>
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="bankName">Bank name</Label>
                    <Input
                      id="bankName"
                      value={bankName}
                      onChange={(event) => setBankName(event.target.value)}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="bankAccountNumber">Bank account number</Label>
                    <Input
                      id="bankAccountNumber"
                      type="password"
                      autoComplete="off"
                      placeholder={
                        profile?.bankAccountMasked
                          ? `Saved ${profile.bankAccountMasked} — enter to replace`
                          : ""
                      }
                      value={bankAccountNumber}
                      onChange={(event) => setBankAccountNumber(event.target.value)}
                    />
                  </div>
                </>
              ) : null}

              {step === "documents" ? (
                <div className="space-y-2">
                  <Label htmlFor="documentKeys">
                    Document object keys (one per line)
                  </Label>
                  <textarea
                    id="documentKeys"
                    rows={6}
                    className="flex min-h-[120px] w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-xs"
                    placeholder="billing/payout-docs/your-org/id-front.pdf"
                    value={documentKeysText}
                    onChange={(event) => setDocumentKeysText(event.target.value)}
                  />
                  <p className="text-xs text-muted-foreground">
                    Upload files to private storage first (same pattern as catalog
                    masters), then paste the storage keys here.
                  </p>
                </div>
              ) : null}

              {step === "review" ? (
                <dl className="grid gap-3 text-sm sm:grid-cols-2">
                  <div>
                    <dt className="text-muted-foreground">Entity</dt>
                    <dd>{legalEntityType} · {legalName}</dd>
                  </div>
                  <div>
                    <dt className="text-muted-foreground">Address</dt>
                    <dd>
                      {addressLine1}, {city} {postalCode}, {countryCode}
                    </dd>
                  </div>
                  <div>
                    <dt className="text-muted-foreground">Payout rail</dt>
                    <dd>{payoutRail}</dd>
                  </div>
                  <div>
                    <dt className="text-muted-foreground">Bank</dt>
                    <dd>
                      {bankName || "—"}{" "}
                      {profile?.bankAccountMasked ? `(${profile.bankAccountMasked})` : ""}
                    </dd>
                  </div>
                  <div className="sm:col-span-2">
                    <dt className="text-muted-foreground">Documents</dt>
                    <dd>{documentObjectKeys.length} file(s)</dd>
                  </div>
                </dl>
              ) : null}

              <div className="flex flex-wrap gap-2 pt-2">
                <Button
                  type="button"
                  variant="outline"
                  disabled={stepIndex <= 0}
                  onClick={() => setStep(steps[stepIndex - 1]!)}
                >
                  Back
                </Button>
                {stepIndex < steps.length - 1 ? (
                  <Button
                    type="button"
                    onClick={() => setStep(steps[stepIndex + 1]!)}
                  >
                    Next
                  </Button>
                ) : null}
                <Button type="button" variant="secondary" disabled={saving} onClick={() => void saveDraft()}>
                  {saving ? "Saving…" : "Save draft"}
                </Button>
                {step === "review" && profile?.verificationStatus !== "verified" ? (
                  <Button
                    type="button"
                    disabled={submitting}
                    onClick={() => void onSubmitForReview()}
                  >
                    {submitting ? "Submitting…" : "Submit for review"}
                  </Button>
                ) : null}
              </div>
            </CardContent>
          </Card>
        </>
      ) : null}

      {!canManage ? (
        <p className="text-sm text-muted-foreground">
          You can view payout status but need{" "}
          <code className="text-xs">manage:payout:profile:all</code> to edit.
        </p>
      ) : null}

      <Button render={<Link href="/dashboard" />} variant="link" className="w-fit px-0">
        Back to dashboard
      </Button>
    </div>
  );
}
