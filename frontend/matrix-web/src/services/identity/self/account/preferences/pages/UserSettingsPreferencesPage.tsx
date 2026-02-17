import UserSettingsSection from "../../shared/components/UserSettingsSection";
import PreferencesCard from "../components/PreferencesCard";

const UserSettingsPreferencesPage = () => {
    return (
        <UserSettingsSection
            title="Workspace preferences"
            subtitle="Tune how this device presents the control panel, starting with language and theme presets."
        >
            <PreferencesCard/>
        </UserSettingsSection>
    );
};

export default UserSettingsPreferencesPage;
