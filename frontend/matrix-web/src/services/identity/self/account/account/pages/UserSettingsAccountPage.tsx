import {useAuth} from "@services/identity/api/self/auth/AuthContext";
import UserSettingsSection from "../../shared/components/UserSettingsSection";
import AccountCard from "../components/AccountCard";

const UserSettingsAccountPage = () => {
    const {user} = useAuth();

    return (
        <UserSettingsSection
            title="Account"
            subtitle="Review username and email without mixing identity data with avatar personalization or device-local workspace preferences."
        >
            <AccountCard
                username={user?.username ?? ""}
                email={user?.email ?? ""}
                isEmailConfirmed={user?.isEmailConfirmed ?? false}
            />
        </UserSettingsSection>
    );
};

export default UserSettingsAccountPage;
