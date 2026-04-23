import {useAuth} from "@services/identity/api/self/auth/useAuth";
import UserSettingsSection from "../../shared/components/UserSettingsSection";
import DangerZoneCard from "../components/DangerZoneCard";

const UserSettingsDangerPage = () => {
    const {token} = useAuth();

    return (
        <UserSettingsSection
            title="Danger zone"
            subtitle="High-impact actions that disable access or require deliberate recovery."
        >
            <DangerZoneCard token={token}/>
        </UserSettingsSection>
    );
};

export default UserSettingsDangerPage;
