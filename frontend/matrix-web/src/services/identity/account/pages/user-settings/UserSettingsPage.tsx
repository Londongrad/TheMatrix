// src/services/identity/account/pages/user-settings/UserSettingsPage.tsx
import { useAuth } from "@services/identity/api/auth/AuthContext";
import { useConfirm } from "@shared/components/ConfirmDialog";
import "@services/identity/account/styles/user-settings-page.css";

import ProfileCard from "./components/ProfileCard";
import SecurityCard from "./components/SecurityCard";
import SessionsCard from "./components/SessionsCard";
import PreferencesCard from "./components/PreferencesCard";
import DangerZoneCard from "./components/DangerZoneCard";

const UserSettingsPage = () => {
  const { user, token, patchUser, logout } = useAuth();
  const confirm = useConfirm();

  return (
    <div className="user-settings-page">
      <div className="user-settings-header">
        <div>
          <h1 className="user-settings-title">User settings</h1>
          <p className="user-settings-subtitle">
            Adjust your Overseer identity, security and control panel
            preferences.
          </p>
        </div>
      </div>

      <div className="user-settings-grid">
        <ProfileCard
          token={token}
          avatarUrl={user?.avatarUrl ?? undefined}
          initialUsername={user?.username ?? ""}
          initialEmail={user?.email ?? ""}
          patchUser={patchUser}
        />

        <SecurityCard token={token} />

        <SessionsCard token={token} logout={logout} confirm={confirm} />

        <PreferencesCard />

        <DangerZoneCard token={token} />
      </div>
    </div>
  );
};

export default UserSettingsPage;
