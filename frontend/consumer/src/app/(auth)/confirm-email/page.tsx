"use client";

import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Text } from "@/components/ui/Text";
import { confirmEmail } from "@/lib/api/identityClient";
import { ApiError } from "@/lib/api/types";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState } from "react";

function ConfirmEmailContent() {
  const searchParams = useSearchParams();
  const userId = searchParams.get("userId");
  const token = searchParams.get("token");
  const [status, setStatus] = useState<"loading" | "success" | "error">(
    "loading",
  );
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!userId || !token) {
      setStatus("error");
      setMessage("Confirmation link is incomplete.");
      return;
    }

    void (async () => {
      try {
        await confirmEmail(userId, token);
        setStatus("success");
        setMessage("Your email is confirmed. You can sign in now.");
      } catch (err) {
        setStatus("error");
        setMessage(
          err instanceof ApiError
            ? err.message
            : err instanceof Error
              ? err.message
              : "Confirmation failed.",
        );
      }
    })();
  }, [userId, token]);

  return (
    <div className="flex min-h-dvh flex-col items-center justify-center bg-background p-6">
      <Card className="w-full max-w-md">
        <div className="flex flex-col gap-4">
          <Text as="h1" variant="headline-large">
            Email confirmation
          </Text>
          {status === "loading" ? (
            <Text variant="body-medium">Confirming your email…</Text>
          ) : (
            <Text
              variant="body-medium"
              className={status === "error" ? "text-error" : undefined}
            >
              {message}
            </Text>
          )}
          {status !== "loading" ? (
            <Link href="/login">
              <Button type="button">Go to sign in</Button>
            </Link>
          ) : null}
        </div>
      </Card>
    </div>
  );
}

export default function ConfirmEmailPage() {
  return (
    <Suspense fallback={null}>
      <ConfirmEmailContent />
    </Suspense>
  );
}
