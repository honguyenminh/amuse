import type { NetworkHints } from "./selectRendition";

export function getNetworkHints(): NetworkHints {
  if (typeof navigator === "undefined") return {};
  const connection = (
    navigator as Navigator & {
      connection?: { effectiveType?: string; saveData?: boolean };
    }
  ).connection;
  return {
    effectiveType: connection?.effectiveType,
    saveData: connection?.saveData,
  };
}
