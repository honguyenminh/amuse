import { Text } from "@/components/ui/Text";
import type { OrganizationTrustTier } from "@/lib/api/types";

type UnverifiedSellerBadgeProps = {
  trustTier: OrganizationTrustTier | string | null | undefined;
};

export function UnverifiedSellerBadge({ trustTier }: UnverifiedSellerBadgeProps) {
  if (trustTier !== "unverified") {
    return null;
  }

  return (
    <span className="inline-flex items-center rounded-full border border-outline/60 bg-surface-container-high px-2 py-0.5">
      <Text variant="label-medium" className="text-on-surface-variant">
        Unverified seller
      </Text>
    </span>
  );
}
