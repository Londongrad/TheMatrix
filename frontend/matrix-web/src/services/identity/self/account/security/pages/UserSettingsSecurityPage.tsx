import { useAuth } from "@services/identity/api/self/auth/AuthContext";
import UserSettingsSection from "../../shared/components/UserSettingsSection";
import SecurityCard from "../components/SecurityCard";

const UserSettingsSecurityPage = () => {
  const { token } = useAuth();

  return (
    <UserSettingsSection
      title="Security"
      subtitle="Harden access with passwords and verification signals for your account."
    >
      <SecurityCard token={token} />
    </UserSettingsSection>
  );
};

export default UserSettingsSecurityPage;
