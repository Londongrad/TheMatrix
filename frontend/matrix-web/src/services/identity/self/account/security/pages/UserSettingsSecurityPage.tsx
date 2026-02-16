import {useAuth} from "@services/identity/api/self/auth/AuthContext";
import {useLocation} from "react-router-dom";
import UserSettingsSection from "../../shared/components/UserSettingsSection";
import SecurityCard from "../components/SecurityCard";

const UserSettingsSecurityPage = () => {
    const {token, user} = useAuth();
    const location = useLocation();
    const emailConfirmationRequested = Boolean(
        (location.state as { emailConfirmationRequested?: boolean } | null)
            ?.emailConfirmationRequested,
    );

    return (
        <UserSettingsSection
            title="Security"
            subtitle="Harden access with passwords and verification signals for your account."
        >
            <SecurityCard
                token={token}
                email={user?.email ?? ""}
                isEmailConfirmed={user?.isEmailConfirmed ?? false}
                emailConfirmationRequested={emailConfirmationRequested}
            />
        </UserSettingsSection>
    );
};

export default UserSettingsSecurityPage;
