import type { ReactNode } from "react";

export default function UserBadge({
  kind,
  children,
}: {
  kind: "ok" | "warn" | "bad" | "info";
  children: ReactNode;
}) {
  return <span className={`mx-admin-users__badge ${kind}`}>{children}</span>;
}
