"use client";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { resendConfirmation } from "@/lib/api/identityClient";
import { ApiError } from "@/lib/api/types";
import { useAuth } from "@/lib/auth/AuthProvider";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState } from "react";

function safeNextPath(value: string | null): string {
  if (!value) {
    return "/dashboard";
  }
  if (!value.startsWith("/") || value.startsWith("//")) {
    return "/dashboard";
  }
  return value;
}

function LoginInner() {
  const auth = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();
  const next = safeNextPath(searchParams.get("next"));
  const [email, setEmail] = useState("root@amuse.local");
  const [password, setPassword] = useState("ChangeMe_Root123!");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [pendingEmail, setPendingEmail] = useState<string | null>(null);
  const [resendMessage, setResendMessage] = useState<string | null>(null);
  const [resending, setResending] = useState(false);

  useEffect(() => {
    if (!auth.isReady) {
      return;
    }
    if (!auth.isAuthenticated) {
      return;
    }
    if (auth.needsPersonaSelection) {
      router.replace("/select-persona");
      return;
    }
    if (auth.activePersona) {
      router.replace(next);
    }
  }, [
    auth.isReady,
    auth.isAuthenticated,
    auth.needsPersonaSelection,
    auth.activePersona,
    next,
    router,
  ]);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setLoading(true);
    setError(null);
    setResendMessage(null);
    setPendingEmail(null);
    try {
      const result = await auth.login(email, password);
      if (result.needsOrganizationSetup) {
        router.replace("/create-organization?returnTo=/dashboard");
        return;
      }
      router.replace(result.needsSelection ? "/select-persona" : next);
    } catch (err) {
      if (err instanceof ApiError && err.code === "identity.email_not_confirmed") {
        setPendingEmail(email);
        setError(err.message);
      } else {
        setError(err instanceof Error ? err.message : "Login failed.");
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="flex min-h-dvh items-center justify-center bg-muted/40 p-6">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>Sign in to Amuse Console</CardTitle>
          <CardDescription>
            Business and platform administration portal.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form className="flex flex-col gap-4" onSubmit={onSubmit}>
            <div className="grid gap-2">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                required
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                required
              />
            </div>
            {error ? (
              <p className="text-sm text-destructive">{error}</p>
            ) : null}
            <Button type="submit" disabled={loading}>
              {loading ? "Signing in…" : "Sign in"}
            </Button>
            {pendingEmail ? (
              <Button
                type="button"
                variant="outline"
                disabled={resending}
                onClick={() => {
                  void (async () => {
                    setResending(true);
                    setResendMessage(null);
                    try {
                      const result = await resendConfirmation(
                        pendingEmail,
                        "business",
                      );
                      setResendMessage(result.message);
                    } catch (resendErr) {
                      setResendMessage(
                        resendErr instanceof Error
                          ? resendErr.message
                          : "Could not resend confirmation email.",
                      );
                    } finally {
                      setResending(false);
                    }
                  })();
                }}
              >
                {resending ? "Sending…" : "Resend confirmation email"}
              </Button>
            ) : null}
            {resendMessage ? (
              <p className="text-sm text-muted-foreground">{resendMessage}</p>
            ) : null}
            <p className="text-center text-sm text-muted-foreground">
              New here?{" "}
              <a href="/signup" className="underline-offset-4 hover:underline">
                Create an account
              </a>
            </p>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

export default function LoginPage() {
  return (
    <Suspense fallback={null}>
      <LoginInner />
    </Suspense>
  );
}
