import type { CatalogPricingResponse } from "@/lib/api/types";

const DEFAULT_CURRENCY = "USD";

function currencyCode(pricing: CatalogPricingResponse): string {
  return pricing.priceCurrency?.trim() || DEFAULT_CURRENCY;
}

function formatMinor(minor: number, currency: string): string {
  try {
    return new Intl.NumberFormat(undefined, {
      style: "currency",
      currency,
    }).format(minor / 100);
  } catch {
    return `${(minor / 100).toFixed(2)} ${currency}`;
  }
}

export function isFreeEligible(pricing: CatalogPricingResponse | null | undefined): boolean {
  return pricing?.isForSale === true && pricing.priceFloorMinor === 0;
}

export function isPaidOnly(pricing: CatalogPricingResponse | null | undefined): boolean {
  return pricing?.isForSale === true && pricing.priceFloorMinor > 0;
}

/** Default paid checkout amount: fixed price or PWYW floor. */
export function defaultCheckoutAmountMinor(
  pricing: CatalogPricingResponse | null | undefined,
): number | null {
  if (!pricing?.isForSale || pricing.priceFloorMinor <= 0) return null;
  if (pricing.priceCeilingMinor != null && pricing.priceCeilingMinor < pricing.priceFloorMinor) {
    return null;
  }
  return pricing.priceFloorMinor;
}

export function formatPricingLabel(pricing: CatalogPricingResponse): string {
  const currency = currencyCode(pricing);
  const floor = formatMinor(pricing.priceFloorMinor, currency);

  if (pricing.priceFloorMinor === 0 && pricing.priceCeilingMinor === null) {
    return "Free or pay what you want";
  }

  if (pricing.priceFloorMinor === 0 && pricing.priceCeilingMinor === 0) {
    return "Free";
  }

  if (pricing.priceFloorMinor === 0 && pricing.priceCeilingMinor != null) {
    const ceiling = formatMinor(pricing.priceCeilingMinor, currency);
    return `Free – ${ceiling} max`;
  }

  if (pricing.priceCeilingMinor == null) {
    return `${floor}+`;
  }

  if (pricing.priceCeilingMinor === pricing.priceFloorMinor) {
    return floor;
  }

  const ceiling = formatMinor(pricing.priceCeilingMinor, currency);
  return `${floor} – ${ceiling}`;
}
