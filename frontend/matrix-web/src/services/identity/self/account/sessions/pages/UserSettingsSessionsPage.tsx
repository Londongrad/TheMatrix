import {useAuth} from "@services/identity/api/self/auth/AuthContext";
import {useConfirm} from "@shared/ui/components/ConfirmDialog/ConfirmDialog";
import UserSettingsSection from "../../shared/components/UserSettingsSection";
import UserSettingsSessionsCard from "../components/UserSettingsSessionsCard";
import "../styles/user-settings-sessions.css";

const UserSettingsSessionsPage = () => {
    const {token, logout} = useAuth();
    const confirm = useConfirm();

    return (
        <UserSettingsSection
            title="Sessions"
            subtitle="Review signed-in devices, spot the current session, and revoke the ones you no longer need."
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
