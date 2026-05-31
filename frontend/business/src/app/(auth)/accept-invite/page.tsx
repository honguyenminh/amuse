import { Suspense } from "react";
import { AcceptInviteContent } from "./AcceptInviteContent";

export default function AcceptInvitePage() {
  return (
    <Suspense fallback={<p className="p-6 text-sm text-muted-foreground">Loading invitation…</p>}>
      <AcceptInviteContent />
    </Suspense>
  );
}
