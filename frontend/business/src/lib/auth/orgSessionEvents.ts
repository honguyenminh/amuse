export const ORGANIZATION_UNAVAILABLE_CODE = "tenancy.organization_not_found";

type OrgUnavailableHandler = (message: string) => void | Promise<void>;

let handler: OrgUnavailableHandler | null = null;

export function setOrgUnavailableHandler(next: OrgUnavailableHandler | null): void {
  handler = next;
}

export function notifyOrgUnavailable(message: string): void {
  void handler?.(message);
}

export function isOrganizationUnavailableError(code: string | undefined): boolean {
  return code === ORGANIZATION_UNAVAILABLE_CODE;
}
