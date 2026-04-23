import {useAuth} from "@services/identity/api/self/auth/useAuth";
import UserSettingsSection from "../../shared/components/UserSettingsSection";
import AccountCard from "../components/AccountCard";

const UserSettingsAccountPage = () => {
    const {user, patchUser} = useAuth();

    return (
        <UserSettingsSection
            title="Account"
            subtitle="Manage the core sign-in identity of this operator account without duplicating personalization or recovery-email workflows."
        >
            <AccountCard
                userId={user?.userId ?? ""}
                username={user?.username ?? ""}
                pendingEmail={user?.pendingEmail ?? null}
                isEmailConfirmed={user?.isEmailConfirmed ?? false}
                createdAtUtc={user?.createdAtUtc ?? ""}
                emailConfirmedAtUtc={user?.emailConfirmedAtUtc ?? null}
                patchUser={patchUser}
            />
        </UserSettingsSection>
    );
};

export default UserSettingsAccountPage;
