"use client";

import {
  getCurrentAccount,
  listPersonas,
  loginPassword,
  refreshTokens,
  revokeSession,
} from "@/lib/api/identityClient";
import type {
  AvailablePersona,
  CurrentAccountResponse,
  PersonaContextRequest,
} from "@/lib/api/types";
import {
  filterBusinessPersonas,
  personaMatchesContext,
  personaToContext,
} from "@/lib/auth/resolveBusinessPersonas";
import { listenerBootstrapContext } from "@/lib/auth/listenerBootstrapContext";
import { recordRecentPersona } from "@/lib/auth/recentPersonas";
import {
  clearSession,
  getAccessToken,
  getActivePersona,
  readStoredPersona,
  setAccessToken,
  setActivePersona,
} from "@/lib/auth/sessionStore";
import { usePathname, useRouter } from "next/navigation";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";

type AuthState = {
  isReady: boolean;
  isAuthenticated: boolean;
  account: CurrentAccountResponse | null;
  activePersona: PersonaContextRequest | null;
  businessPersonas: AvailablePersona[];
  needsPersonaSelection: boolean;
  needsOrganizationSetup: boolean;
  bootstrapError: string | null;
  login: (email: string, password: string) => Promise<{
    needsSelection: boolean;
    needsOrganizationSetup: boolean;
  }>;
  logout: () => Promise<void>;
  selectPersona: (persona: AvailablePersona) => Promise<void>;
  reloadBusinessPersonas: () => Promise<AvailablePersona[]>;
  retryBootstrap: () => Promise<void>;
};

const AuthContext = createContext<AuthState | null>(null);

async function activatePersona(
  token: string,
  context: PersonaContextRequest,
): Promise<string> {
  const refreshed = await refreshTokens(context);
  setAccessToken(refreshed.accessToken);
  setActivePersona(context);
  return refreshed.accessToken;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const [isReady, setIsReady] = useState(false);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [account, setAccount] = useState<CurrentAccountResponse | null>(null);
  const [activePersonaState, setActivePersonaState] =
    useState<PersonaContextRequest | null>(null);
  const [businessPersonas, setBusinessPersonas] = useState<AvailablePersona[]>(
    [],
  );
  const [bootstrapError, setBootstrapError] = useState<string | null>(null);

  const resolveSession = useCallback(async (initialToken: string) => {
    setBootstrapError(null);

    const personas = filterBusinessPersonas(
      await listPersonas(initialToken),
    );
    setBusinessPersonas(personas);

    if (personas.length === 0) {
      setActivePersona(null);
      setActivePersonaState(null);
      setBootstrapError(null);
      return {
        token: initialToken,
        activePersona: null,
        needsSelection: false,
        needsOrganizationSetup: true,
        error: null,
      };
    }

    const stored = readStoredPersona();
    const storedMatch =
      stored &&
      personas.find((persona) => personaMatchesContext(persona, stored));

    if (storedMatch) {
      const context = personaToContext(storedMatch);
      const token = await activatePersona(initialToken, context);
      recordRecentPersona(storedMatch);
      setActivePersonaState(context);
      return {
        token,
        activePersona: context,
        needsSelection: false,
        needsOrganizationSetup: false,
        error: null,
      };
    }

    if (personas.length === 1) {
      const only = personas[0]!;
      const context = personaToContext(only);
      const token = await activatePersona(initialToken, context);
      recordRecentPersona(only);
      setActivePersonaState(context);
      return {
        token,
        activePersona: context,
        needsSelection: false,
        needsOrganizationSetup: false,
        error: null,
      };
    }

    setActivePersona(null);
    setActivePersonaState(null);
    return {
      token: initialToken,
      activePersona: null,
      needsSelection: true,
      needsOrganizationSetup: false,
      error: null,
    };
  }, []);

  const loadAccount = useCallback(async (token: string) => {
    const me = await getCurrentAccount(token);
    setAccount(me);
  }, []);

  const restoreSession = useCallback(async () => {
    try {
      const refreshed = await refreshTokens(listenerBootstrapContext);
      setAccessToken(refreshed.accessToken);
      const resolved = await resolveSession(refreshed.accessToken);
      await loadAccount(resolved.token);
      setIsAuthenticated(true);
    } catch {
      clearSession();
      setIsAuthenticated(false);
      setAccount(null);
      setActivePersonaState(null);
      setBusinessPersonas([]);
    } finally {
      setIsReady(true);
    }
  }, [loadAccount, resolveSession]);

  useEffect(() => {
    // Session restore on mount is intentional one-shot bootstrap.
    // eslint-disable-next-line react-hooks/set-state-in-effect -- auth bootstrap
    void restoreSession();
  }, [restoreSession]);

  const needsOrganizationSetup =
    isAuthenticated &&
    businessPersonas.length === 0 &&
    activePersonaState === null &&
    bootstrapError === null;

  useEffect(() => {
    if (!isReady || !needsOrganizationSetup) {
      return;
    }
    if (
      pathname.startsWith("/create-organization") ||
      pathname.startsWith("/login") ||
      pathname.startsWith("/signup") ||
      pathname.startsWith("/confirm-email") ||
      pathname.startsWith("/accept-invite")
    ) {
      return;
    }
    router.replace("/create-organization?returnTo=/dashboard");
  }, [isReady, needsOrganizationSetup, pathname, router]);

  const login = useCallback(
    async (email: string, password: string) => {
      setBootstrapError(null);
      const tokens = await loginPassword(
        email,
        password,
        listenerBootstrapContext,
      );
      setAccessToken(tokens.accessToken);
      const resolved = await resolveSession(tokens.accessToken);
      if (resolved.error) {
        throw new Error(resolved.error);
      }
      await loadAccount(resolved.token);
      setIsAuthenticated(true);
      return {
        needsSelection: resolved.needsSelection,
        needsOrganizationSetup: resolved.needsOrganizationSetup ?? false,
      };
    },
    [loadAccount, resolveSession],
  );

  const logout = useCallback(async () => {
    const token = getAccessToken();
    try {
      if (token) {
        await revokeSession(token);
      }
    } finally {
      clearSession();
      setIsAuthenticated(false);
      setAccount(null);
      setActivePersonaState(null);
      setBusinessPersonas([]);
      setBootstrapError(null);
    }
  }, []);

  const selectPersona = useCallback(
    async (persona: AvailablePersona) => {
      const token = getAccessToken();
      if (!token) {
        throw new Error("Not authenticated.");
      }
      const context = personaToContext(persona);
      const refreshedToken = await activatePersona(token, context);
      recordRecentPersona(persona);
      setActivePersonaState(context);
      setBootstrapError(null);
      await loadAccount(refreshedToken);
    },
    [loadAccount],
  );

  const reloadBusinessPersonas = useCallback(async () => {
    const token = getAccessToken();
    if (!token) {
      throw new Error("Not authenticated.");
    }
    const personas = filterBusinessPersonas(await listPersonas(token));
    setBusinessPersonas(personas);
    return personas;
  }, []);

  const retryBootstrap = useCallback(async () => {
    const token = getAccessToken();
    if (!token) {
      setBootstrapError("Not authenticated.");
      return;
    }
    try {
      const resolved = await resolveSession(token);
      if (resolved.error) {
        setBootstrapError(resolved.error);
        return;
      }
      await loadAccount(getAccessToken() ?? resolved.token);
    } catch (error) {
      setBootstrapError(
        error instanceof Error ? error.message : "Bootstrap failed.",
      );
    }
  }, [loadAccount, resolveSession]);

  const needsPersonaSelection =
    isAuthenticated &&
    businessPersonas.length > 1 &&
    activePersonaState === null &&
    bootstrapError === null;

  const value = useMemo<AuthState>(
    () => ({
      isReady,
      isAuthenticated,
      account,
      activePersona: activePersonaState ?? getActivePersona(),
      businessPersonas,
      needsPersonaSelection,
      needsOrganizationSetup,
      bootstrapError,
      login,
      logout,
      selectPersona,
      reloadBusinessPersonas,
      retryBootstrap,
    }),
    [
      isReady,
      isAuthenticated,
      account,
      activePersonaState,
      businessPersonas,
      needsPersonaSelection,
      needsOrganizationSetup,
      bootstrapError,
      login,
      logout,
      selectPersona,
      reloadBusinessPersonas,
      retryBootstrap,
    ],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within AuthProvider");
  }
  return ctx;
}
