import { useAuth } from "@services/identity/api/self/auth/AuthContext";
import { useConfirm } from "@shared/ui/components/ConfirmDialog/ConfirmDialog";
import UserSettingsSection from "../components/UserSettingsSection";
import SessionsCard from "@services/identity/self/sessions/components/SessionsCard";

const UserSettingsSessionsPage = () => {
  const { token, logout } = useAuth();
  const confirm = useConfirm();

  return (
    <UserSettingsSection
      title="Sessions"
      subtitle="Track and revoke active devices connected to your Matrix identity."
    >
      <SessionsCard token={token} logout={logout} confirm={confirm} />
    </UserSettingsSection>
  );
};

export default UserSettingsSessionsPage;
