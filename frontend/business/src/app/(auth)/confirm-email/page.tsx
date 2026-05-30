"use client";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
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
        setMessage("Your email is confirmed. You can sign in and set up your organization.");
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
    <div className="flex min-h-dvh items-center justify-center bg-muted/40 p-6">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>Email confirmation</CardTitle>
          <CardDescription>Amuse business portal</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {status === "loading" ? (
            <p className="text-sm text-muted-foreground">Confirming your email…</p>
          ) : (
            <p
              className={
                status === "error" ? "text-sm text-destructive" : "text-sm"
              }
            >
              {message}
            </p>
          )}
          {status !== "loading" ? (
            <Button render={<Link href="/login" />}>Go to sign in</Button>
          ) : null}
        </CardContent>
      </Card>
    </div>
  );
}

export default function ConfirmEmailPage() {
  return (
    <Suspense
      fallback={
        <div className="flex min-h-dvh items-center justify-center p-6">
          <Skeleton className="h-40 w-full max-w-md" />
        </div>
      }
    >
      <ConfirmEmailContent />
    </Suspense>
  );
}
