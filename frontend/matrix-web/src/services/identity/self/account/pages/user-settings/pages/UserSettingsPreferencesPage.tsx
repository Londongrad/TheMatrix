import UserSettingsSection from "../components/UserSettingsSection";
import PreferencesCard from "../components/PreferencesCard";

const UserSettingsPreferencesPage = () => {
  return (
    <UserSettingsSection
      title="Preferences"
      subtitle="Tune display, locale and notification presets for your console."
    >
      <PreferencesCard />
    </UserSettingsSection>
  );
};

export default UserSettingsPreferencesPage;
