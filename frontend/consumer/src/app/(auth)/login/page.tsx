"use client";

import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Text } from "@/components/ui/Text";
import { useAuth } from "@/lib/auth/AuthProvider";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState } from "react";

/**
 * `?next=<path>` lets us bounce visitors back to where they were when they
 * hit a login wall (e.g. pressing play while anonymous). Sanitised to local
 * paths only so a crafted ?next=https://attacker can't redirect off-site.
 */
function safeNextPath(value: string | null): string {
  if (!value) return "/home";
  if (!value.startsWith("/") || value.startsWith("//")) return "/home";
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

  useEffect(() => {
    if (auth.isReady && auth.isAuthenticated) {
      router.replace(next);
    }
  }, [auth.isReady, auth.isAuthenticated, next, router]);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setLoading(true);
    setError(null);
    try {
      await auth.login(email, password);
      router.replace(next);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Login failed.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="flex min-h-dvh flex-col items-center justify-center bg-background p-6">
      <Card className="w-full max-w-md">
        <form className="flex flex-col gap-4" onSubmit={onSubmit}>
          <Text as="h1" variant="headline-large">
            Sign in
          </Text>
          <label className="flex flex-col gap-1">
            <Text variant="label-medium">Email</Text>
            <input
              className="border-2 border-outline bg-surface px-3 py-2 text-body-large text-on-surface"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              autoComplete="email"
              required
            />
          </label>
          <label className="flex flex-col gap-1">
            <Text variant="label-medium">Password</Text>
            <input
              className="border-2 border-outline bg-surface px-3 py-2 text-body-large text-on-surface"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="current-password"
              required
            />
          </label>
          {error ? (
            <Text variant="body-medium" className="text-error">
              {error}
            </Text>
          ) : null}
          <Button type="submit" disabled={loading}>
            {loading ? "Signing in…" : "Sign in"}
          </Button>
        </form>
      </Card>
    </div>
  );
}

export default function LoginPage() {
  // `useSearchParams` requires a Suspense boundary in Next.js 15 static export
  // mode; rendering the form lazily satisfies it without any UX impact since
  // the fallback only flashes for one render.
  return (
    <Suspense fallback={null}>
      <LoginInner />
    </Suspense>
  );
}
