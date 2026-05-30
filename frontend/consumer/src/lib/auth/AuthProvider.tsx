"use client";

import {
  loginPassword,
  refreshTokens,
  revokeSession,
} from "@/lib/api/identityClient";
import { listenerBootstrapContext } from "@/lib/auth/listenerBootstrapContext";
import type { PersonaContextRequest } from "@/lib/api/types";
import { ApiError } from "@/lib/api/types";
import { bootstrapListener } from "@/lib/listener/bootstrapListener";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import {
  clearSession,
  getAccessToken,
  getListenerId,
  setAccessToken,
  setListenerId,
} from "./sessionStore";

type AuthState = {
  isReady: boolean;
  isAuthenticated: boolean;
  listenerId: string | null;
  bootstrapError: string | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  retryBootstrap: () => Promise<void>;
};

const AuthContext = createContext<AuthState | null>(null);

function listenerContext(): PersonaContextRequest {
  const id = getListenerId();
  if (!id) {
    throw new ApiError("Listener profile is not ready.", 400);
  }
  return { type: "listener", orgId: null, listenerId: id };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isReady, setIsReady] = useState(false);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [listenerId, setListenerIdState] = useState<string | null>(null);
  const [bootstrapError, setBootstrapError] = useState<string | null>(null);

  const runBootstrap = useCallback(async (token: string) => {
    setBootstrapError(null);
    try {
      const result = await bootstrapListener(token);
      setListenerId(result.listenerId);
      setListenerIdState(result.listenerId);
    } catch (error) {
      setBootstrapError(
        error instanceof Error ? error.message : "Bootstrap failed.",
      );
      throw error;
    }
  }, []);

  const restoreSession = useCallback(async () => {
    try {
      const refreshed = await refreshTokens(listenerBootstrapContext);
      setAccessToken(refreshed.accessToken);
      await runBootstrap(refreshed.accessToken);
      setIsAuthenticated(true);
    } catch {
      clearSession();
      setIsAuthenticated(false);
      setListenerIdState(null);
    } finally {
      setIsReady(true);
    }
  }, [runBootstrap]);

  useEffect(() => {
    void restoreSession();
  }, [restoreSession]);

  const login = useCallback(
    async (email: string, password: string) => {
      const tokens = await loginPassword(
        email,
        password,
        listenerBootstrapContext,
      );
      setAccessToken(tokens.accessToken);
      await runBootstrap(tokens.accessToken);
      setIsAuthenticated(true);
    },
    [runBootstrap],
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
      setListenerIdState(null);
      setBootstrapError(null);
    }
  }, []);

  const retryBootstrap = useCallback(async () => {
    const token = getAccessToken();
    if (!token) {
      setBootstrapError("Not authenticated.");
      return;
    }
    try {
      await runBootstrap(token);
    } catch (error) {
      setBootstrapError(
        error instanceof Error ? error.message : "Bootstrap failed.",
      );
    }
  }, [runBootstrap]);

  const value = useMemo<AuthState>(
    () => ({
      isReady,
      isAuthenticated,
      listenerId,
      bootstrapError,
      login,
      logout,
      retryBootstrap,
    }),
    [
      isReady,
      isAuthenticated,
      listenerId,
      bootstrapError,
      login,
      logout,
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

export function useRequireAuth(): AuthState & { accessToken: string } {
  const auth = useAuth();
  const token = getAccessToken();
  if (!auth.isReady) {
    return { ...auth, accessToken: "" };
  }
  if (!auth.isAuthenticated || !token) {
    throw new Error("AUTH_REQUIRED");
  }
  return { ...auth, accessToken: token };
}
