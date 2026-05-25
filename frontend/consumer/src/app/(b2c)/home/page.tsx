"use client";

import { AppShell } from "@/components/ui/AppShell";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Text } from "@/components/ui/Text";
import { PlaybackDemo } from "@/components/PlaybackDemo";
import { useAuth } from "@/lib/auth/AuthProvider";
import { useRouter } from "next/navigation";

export default function HomePage() {
  const auth = useAuth();
  const router = useRouter();

  return (
    <AppShell
      title="Home"
      activePath="/home"
      trailing={
        <Button type="button" variant="text" onClick={() => void auth.logout().then(() => router.replace("/login"))}>
          Log out
        </Button>
      }
    >
      <div className="flex flex-col gap-4 p-4">
        <Card>
          <Text variant="headline-medium">Listener home</Text>
          <Text variant="body-medium">
            Listener ID: {auth.listenerId ?? "—"}
          </Text>
        </Card>
        <PlaybackDemo />
        <Text variant="body-medium">
          Navigate to Artist or Album via the bottom bar to see page-level color seed overrides.
        </Text>
      </div>
    </AppShell>
  );
}
