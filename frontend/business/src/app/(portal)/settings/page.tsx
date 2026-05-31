"use client";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import Link from "next/link";

export default function SettingsPage() {
  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-4">
      <Card>
        <CardHeader>
          <CardTitle>Settings</CardTitle>
          <CardDescription>
            Account and workspace preferences for the business portal.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-6">
          <section className="flex flex-col gap-2">
            <h2 className="text-sm font-medium">Organizations</h2>
            <p className="text-sm text-muted-foreground">
              Create another indie group or backing organization on your signed-in
              account. Platform operators can still review backing org applications
              from Applications.
            </p>
            <Button render={<Link href="/create-organization?returnTo=/settings" />}>
              Add organization
            </Button>
          </section>
        </CardContent>
      </Card>
    </div>
  );
}
