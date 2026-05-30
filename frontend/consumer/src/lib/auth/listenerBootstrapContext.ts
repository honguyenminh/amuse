import type { PersonaContextRequest } from "@/lib/api/types";

export const listenerBootstrapContext: PersonaContextRequest = {
  type: "listener",
  orgId: null,
  listenerId: null,
};
