import { listPersonas } from "@/lib/api/identityClient";
import { ensureListenerProfile } from "@/lib/api/listenerClient";
import { ApiError } from "@/lib/api/types";

export type BootstrapListenerResult = {
  listenerId: string;
  created: boolean;
};

export async function bootstrapListener(
  accessToken: string,
): Promise<BootstrapListenerResult> {
  const ensured = await ensureListenerProfile(accessToken);
  const personas = await listPersonas(accessToken);
  const listener = personas.find(
    (p) => p.type === "listener" && p.listenerId,
  );

  if (!listener?.listenerId) {
    throw new ApiError(
      "Listener persona is not available after ensure.",
      500,
      "listener.persona_missing",
    );
  }

  return {
    listenerId: listener.listenerId,
    created: ensured.created,
  };
}
