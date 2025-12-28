// src/services/identity/account/pages/user-settings/components/PreferencesCard.tsx
import React, { useState } from "react";

const PreferencesCard = () => {
  const [language, setLanguage] = useState<"en" | "ru">("en");
  const [theme, setTheme] = useState<"dark" | "light">("dark");

  const [isSavingPreferences, setIsSavingPreferences] = useState(false);
  const [preferencesSaved, setPreferencesSaved] = useState(false);

  const simulateSave = () => {
    setIsSavingPreferences(true);
    setPreferencesSaved(false);

    setTimeout(() => {
      setIsSavingPreferences(false);
      setPreferencesSaved(true);
      setTimeout(() => setPreferencesSaved(false), 2000);
    }, 700);
  };

  const handlePreferencesSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    // TODO: позже можно сохранять в localStorage и/или дергать API
    simulateSave();
  };

  return (
    <section className="settings-card">
      <div className="settings-card-header">
        <div>
          <h2 className="settings-card-title">Preferences</h2>
          <p className="settings-card-description">
            Customize your language and theme for the Matrix control panel.
          </p>
        </div>
      </div>

      <form className="settings-form" onSubmit={handlePreferencesSubmit}>
        <div className="settings-field">
          <div className="settings-label-row">
            <label className="settings-label" htmlFor="language">
              Language
            </label>
            <span>Interface language</span>
          </div>
          <select
            id="language"
            className="settings-input settings-select"
            value={language}
            onChange={(e) => setLanguage(e.target.value as "en" | "ru")}
          >
            <option value="en">English (EN)</option>
            <option value="ru">Русский (RU)</option>
          </select>
        </div>

        <div className="settings-field">
          <div className="settings-label-row">
            <span className="settings-label">Theme</span>
            <span>Dark mode is currently active</span>
          </div>

          <label className="settings-switch">
            <input
              type="checkbox"
              checked={theme === "dark"}
              onChange={(e) => setTheme(e.target.checked ? "dark" : "light")}
            />
            <span className="settings-switch-track">
              <span className="settings-switch-thumb" />
            </span>
            <span className="settings-switch-text">
              {theme === "dark" ? "Dark theme (default)" : "Light theme (stub)"}
            </span>
          </label>

          <p className="settings-hint">
            Theme toggle is visual only for now. In the future it will switch
            the entire control panel between dark and light modes.
          </p>
        </div>

        <div className="settings-actions-row">
          {preferencesSaved && (
            <span className="settings-save-badge">Saved</span>
          )}
          <button
            type="submit"
            className="settings-button"
            disabled={isSavingPreferences}
          >
            {isSavingPreferences ? "Saving..." : "Save preferences"}
          </button>
        </div>
      </form>
    </section>
  );
};

export default PreferencesCard;
