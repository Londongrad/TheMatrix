import { useAuth } from "@services/identity/api/self/auth/AuthContext";
import UserSettingsSection from "../components/UserSettingsSection";
import DangerZoneCard from "../components/DangerZoneCard";

const UserSettingsDangerPage = () => {
  const { token } = useAuth();

  return (
    <UserSettingsSection
      title="Danger zone"
      subtitle="High-impact actions that can permanently change your account."
    >
      <DangerZoneCard token={token} />
    </UserSettingsSection>
  );
};

export default UserSettingsDangerPage;
