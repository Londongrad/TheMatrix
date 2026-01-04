// src/services/identity/self/account/profile/pages/UserSettingsProfilePage.tsx
import { useAuth } from "@services/identity/api/self/auth/AuthContext";

import UserSettingsSection from "../../shared/components/UserSettingsSection";

import ProfileCard from "../components/ProfileCard";

const UserSettingsProfilePage = () => {
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

export default UserSettingsProfilePage;
