import { useAuth } from "@services/identity/api/self/auth/AuthContext";
import { useConfirm } from "@shared/ui/components/ConfirmDialog/ConfirmDialog";
import UserSettingsSection from "../../shared/components/UserSettingsSection";
import UserSettingsSessionsCard from "../components/UserSettingsSessionsCard";
import "../styles/user-settings-sessions.css";

const UserSettingsSessionsPage = () => {
  const { token, logout } = useAuth();
  const confirm = useConfirm();

  return (
    <UserSettingsSection
      title="Sessions"
      subtitle="Track and revoke active devices connected to your Matrix identity."
    >
      <UserSettingsSessionsCard
        token={token}
        logout={logout}
        confirm={confirm}
      />
    </UserSettingsSection>
  );
};

export default UserSettingsSessionsPage;
