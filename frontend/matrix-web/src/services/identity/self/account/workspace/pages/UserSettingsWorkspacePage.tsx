import UserSettingsSection from "../../shared/components/UserSettingsSection";
import PreferencesCard from "../../preferences/components/PreferencesCard";

const UserSettingsWorkspacePage = () => {
    return (
        <UserSettingsSection
            title="Workspace"
            subtitle="Tune language and theme presets for this device without confusing local UI preferences with account data."
        >
            <PreferencesCard/>
        </UserSettingsSection>
    );
};

export default UserSettingsWorkspacePage;
