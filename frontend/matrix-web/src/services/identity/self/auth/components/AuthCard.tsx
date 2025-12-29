import type { ReactNode } from "react";

import "@services/identity/self/auth/styles/auth-page.css";

type Props = {
  side: ReactNode;
  children: ReactNode;
};

export default function AuthCard({ side, children }: Props) {
  return (
    <div className="auth-card">
      <div className="auth-card__side">{side}</div>
      <div className="auth-card__form">{children}</div>
    </div>
  );
}
