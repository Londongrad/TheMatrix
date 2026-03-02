import {useAuth} from "@services/identity/api/self/auth/AuthContext";
import UserSettingsSection from "../../shared/components/UserSettingsSection";
import PersonalizationCard from "../components/PersonalizationCard";

const UserSettingsPersonalizationPage = () => {
    const {user, token, patchUser} = useAuth();

    return (
        <UserSettingsSection
            title="Personalization"
            subtitle="Keep avatar and other appearance choices separate from account identity and security controls."
        >
            <PersonalizationCard
                token={token}
                avatarUrl={user?.avatarUrl ?? undefined}
                displayName={user?.displayName ?? undefined}
                username={user?.username ?? ""}
                email={user?.email ?? ""}
                patchUser={patchUser}
            />
        </UserSettingsSection>
    );
};

export default UserSettingsPersonalizationPage;
