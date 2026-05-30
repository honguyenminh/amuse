"use client";

import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Text } from "@/components/ui/Text";
import { registerPassword } from "@/lib/api/identityClient";
import { ApiError } from "@/lib/api/types";
import Link from "next/link";
import { useState } from "react";

function isPasswordValid(password: string): boolean {
  return (
    password.length >= 10 &&
    /[0-9]/.test(password) &&
    /[A-Z]/.test(password) &&
    /[a-z]/.test(password) &&
    /[^A-Za-z0-9]/.test(password)
  );
}

export default function SignupPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (password !== confirmPassword) {
      setError("Passwords do not match.");
      return;
    }
    if (!isPasswordValid(password)) {
      setError(
        "Password must be at least 10 characters and include upper, lower, digit, and symbol.",
      );
      return;
    }

    setLoading(true);
    setError(null);
    setSuccess(null);
    try {
      const result = await registerPassword(email, password, "consumer");
      setSuccess(result.message);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError(err instanceof Error ? err.message : "Registration failed.");
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="flex min-h-dvh flex-col items-center justify-center bg-background p-6">
      <Card className="w-full max-w-md">
        {success ? (
          <div className="flex flex-col gap-4">
            <Text as="h1" variant="headline-large">
              Check your email
            </Text>
            <Text variant="body-medium">{success}</Text>
            <Link href="/login" className="text-body-medium underline">
              Back to sign in
            </Link>
          </div>
        ) : (
          <form className="flex flex-col gap-4" onSubmit={onSubmit}>
            <Text as="h1" variant="headline-large">
              Create account
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
                autoComplete="new-password"
                required
              />
            </label>
            <label className="flex flex-col gap-1">
              <Text variant="label-medium">Confirm password</Text>
              <input
                className="border-2 border-outline bg-surface px-3 py-2 text-body-large text-on-surface"
                type="password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                autoComplete="new-password"
                required
              />
            </label>
            {error ? (
              <Text variant="body-medium" className="text-error">
                {error}
              </Text>
            ) : null}
            <Button type="submit" disabled={loading}>
              {loading ? "Creating account…" : "Sign up"}
            </Button>
            <Text variant="body-medium">
              Already have an account?{" "}
              <Link href="/login" className="underline">
                Sign in
              </Link>
            </Text>
          </form>
        )}
      </Card>
    </div>
  );
}
