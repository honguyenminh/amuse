"use client";

import {
  getCurrentAccount,
  listPersonas,
  loginPassword,
  refreshTokens,
  revokeSession,
} from "@/lib/api/identityClient";
import {
  getPortalProfile,
  updatePortalProfile,
  type BusinessPortalProfileResponse,
  type UpdateBusinessPortalProfileRequest,
} from "@/lib/api/tenancyClient";
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
import { setOrgUnavailableHandler } from "@/lib/auth/orgSessionEvents";
import {
  clearSession,
  getAccessToken,
  getActivePersona,
  readStoredPersona,
  setAccessToken,
  setActivePersona,
} from "@/lib/auth/sessionStore";
import { useRouter } from "next/navigation";
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
  portalProfile: BusinessPortalProfileResponse | null;
  activePersona: PersonaContextRequest | null;
  businessPersonas: AvailablePersona[];
  needsPersonaSelection: boolean;
  needsOrganizationSetup: boolean;
  needsPortalProfileOnboarding: boolean;
  bootstrapError: string | null;
  orgUnavailableNotice: string | null;
  clearOrgUnavailableNotice: () => void;
  login: (email: string, password: string) => Promise<{
    needsSelection: boolean;
    needsOrganizationSetup: boolean;
  }>;
  logout: () => Promise<void>;
  selectPersona: (persona: AvailablePersona) => Promise<void>;
  reloadBusinessPersonas: () => Promise<AvailablePersona[]>;
  retryBootstrap: () => Promise<void>;
  completePortalProfile: (
    payload: UpdateBusinessPortalProfileRequest,
  ) => Promise<void>;
  refreshPortalProfile: () => Promise<void>;
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
  const [isReady, setIsReady] = useState(false);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [account, setAccount] = useState<CurrentAccountResponse | null>(null);
  const [portalProfile, setPortalProfile] =
    useState<BusinessPortalProfileResponse | null>(null);
  const [activePersonaState, setActivePersonaState] =
    useState<PersonaContextRequest | null>(null);
  const [businessPersonas, setBusinessPersonas] = useState<AvailablePersona[]>(
    [],
  );
  const [bootstrapError, setBootstrapError] = useState<string | null>(null);
  const [orgUnavailableNotice, setOrgUnavailableNotice] = useState<string | null>(
    null,
  );

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
        needsSelection: true,
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

  const loadPortalProfile = useCallback(async () => {
    if (!getActivePersona()) {
      setPortalProfile(null);
      return;
    }
    const profile = await getPortalProfile();
    setPortalProfile(profile);
  }, []);

  const restoreSession = useCallback(async () => {
    try {
      const refreshed = await refreshTokens(listenerBootstrapContext);
      setAccessToken(refreshed.accessToken);
      const resolved = await resolveSession(refreshed.accessToken);
      await loadAccount(resolved.token);
      if (resolved.activePersona) {
        await loadPortalProfile();
      } else {
        setPortalProfile(null);
      }
      setIsAuthenticated(true);
    } catch {
      clearSession();
      setIsAuthenticated(false);
      setAccount(null);
      setPortalProfile(null);
      setActivePersonaState(null);
      setBusinessPersonas([]);
    } finally {
      setIsReady(true);
    }
  }, [loadAccount, loadPortalProfile, resolveSession]);

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
      if (resolved.activePersona) {
        await loadPortalProfile();
      } else {
        setPortalProfile(null);
      }
      setIsAuthenticated(true);
      return {
        needsSelection: resolved.needsSelection,
        needsOrganizationSetup: resolved.needsOrganizationSetup ?? false,
      };
    },
    [loadAccount, loadPortalProfile, resolveSession],
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
      setPortalProfile(null);
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
      await loadPortalProfile();
    },
    [loadAccount, loadPortalProfile],
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

  const switchAwayFromOrg = useCallback(
    async (orgId: string | null) => {
      const personas = await reloadBusinessPersonas();
      const nextPersona =
        orgId === null
          ? personas[0]
          : personas.find(
              (persona) => persona.type !== "org" || persona.orgId !== orgId,
            ) ?? personas[0];
      if (nextPersona) {
        await selectPersona(nextPersona);
        router.replace("/dashboard");
      } else {
        setActivePersona(null);
        setActivePersonaState(null);
        router.replace("/select-persona?switch=1&returnTo=/dashboard");
      }
    },
    [reloadBusinessPersonas, selectPersona, router],
  );

  const clearOrgUnavailableNotice = useCallback(() => {
    setOrgUnavailableNotice(null);
  }, []);

  useEffect(() => {
    setOrgUnavailableHandler(async (message) => {
      const closedOrgId =
        activePersonaState?.type === "org" ? activePersonaState.orgId : null;
      setOrgUnavailableNotice(message);
      try {
        await switchAwayFromOrg(closedOrgId);
      } catch {
        router.replace("/select-persona?switch=1&returnTo=/dashboard");
      }
    });
    return () => setOrgUnavailableHandler(null);
  }, [activePersonaState, switchAwayFromOrg, router]);

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

  const refreshPortalProfile = useCallback(async () => {
    await loadPortalProfile();
  }, [loadPortalProfile]);

  const completePortalProfile = useCallback(
    async (payload: UpdateBusinessPortalProfileRequest) => {
      const profile = await updatePortalProfile(payload);
      setPortalProfile(profile);
    },
    [],
  );

  const needsPersonaSelection =
    isAuthenticated &&
    activePersonaState === null &&
    bootstrapError === null;

  const needsPortalProfileOnboarding =
    isAuthenticated &&
    activePersonaState !== null &&
    portalProfile?.onboardingComplete === false;

  const value = useMemo<AuthState>(
    () => ({
      isReady,
      isAuthenticated,
      account,
      portalProfile,
      activePersona: activePersonaState ?? getActivePersona(),
      businessPersonas,
      needsPersonaSelection,
      needsOrganizationSetup,
      needsPortalProfileOnboarding,
      bootstrapError,
      orgUnavailableNotice,
      clearOrgUnavailableNotice,
      login,
      logout,
      selectPersona,
      reloadBusinessPersonas,
      retryBootstrap,
      completePortalProfile,
      refreshPortalProfile,
    }),
    [
      isReady,
      isAuthenticated,
      account,
      portalProfile,
      activePersonaState,
      businessPersonas,
      needsPersonaSelection,
      needsOrganizationSetup,
      needsPortalProfileOnboarding,
      bootstrapError,
      orgUnavailableNotice,
      clearOrgUnavailableNotice,
      login,
      logout,
      selectPersona,
      reloadBusinessPersonas,
      retryBootstrap,
      completePortalProfile,
      refreshPortalProfile,
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
