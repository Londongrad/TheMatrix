// src/services/identity/account/pages/user-settings/UserSettingsPage.tsx
import { useAuth } from "@services/identity/api/self/auth/AuthContext";

import UserSettingsSection from "../components/UserSettingsSection";

import ProfileCard from "../components/ProfileCard";

const UserSettingsPage = () => {
  const { user, token, patchUser } = useAuth();

  return (
    <UserSettingsSection
      title="Profile"
      subtitle="Manage your display name, email and avatar used across the Matrix console."
    >
      <ProfileCard
        token={token}
        avatarUrl={user?.avatarUrl ?? undefined}
        initialUsername={user?.username ?? ""}
        initialEmail={user?.email ?? ""}
        patchUser={patchUser}
      />
    </UserSettingsSection>
  );
};

export default UserSettingsPage;
