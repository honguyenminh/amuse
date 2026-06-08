import type { NetworkHints } from "./selectRendition";

/** Firefox does not implement navigator.connection; Chromium exposes it behind flags on desktop. */
export function isNetworkInformationAvailable(): boolean {
  return readNavigatorConnection() !== undefined;
}

type NetworkConnection = {
  effectiveType?: string;
  saveData?: boolean;
  downlink?: number;
  addEventListener?: (type: string, listener: () => void) => void;
  removeEventListener?: (type: string, listener: () => void) => void;
};

export function readNavigatorConnection(): NetworkConnection | undefined {
  if (typeof navigator === "undefined") return undefined;
  return (navigator as Navigator & { connection?: NetworkConnection }).connection;
}

export function getNetworkHints(
  extras: Pick<NetworkHints, "throughputKbps" | "stallDowngradeSteps" | "isOwner"> = {},
): NetworkHints {
  const connection = readNavigatorConnection();
  return {
    effectiveType: connection?.effectiveType,
    saveData: connection?.saveData,
    downlinkMbps: connection?.downlink,
    throughputKbps: extras.throughputKbps,
    stallDowngradeSteps: extras.stallDowngradeSteps,
    isOwner: extras.isOwner,
  };
}
