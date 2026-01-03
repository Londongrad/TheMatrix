import type { ReactNode } from "react";
import "@services/identity/self/account/shared/styles/user-settings-page.css";
import "@services/identity/self/account/shared/styles/user-settings-shared.css";

type Props = {
  title: string;
  subtitle: string;
  children: ReactNode;
  layout?: "single" | "grid";
};

export default function UserSettingsSection({
  title,
  subtitle,
  children,
  layout = "single",
}: Props) {
  return (
    <div className="user-settings-page">
      <div className="user-settings-header">
        <div>
          <h1 className="user-settings-title">{title}</h1>
          <p className="user-settings-subtitle">{subtitle}</p>
        </div>
      </div>

      <div
        className={`user-settings-grid${
          layout === "single" ? " user-settings-grid--single" : ""
        }`}
      >
        {children}
      </div>
    </div>
  );
}
