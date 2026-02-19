import {Sparkles, LoaderCircle} from "lucide-react";
import {useEffect, useState, type FormEvent} from "react";
import "@services/identity/self/account/preferences/styles/preferences-card.css";
import {
    useWorkspacePreferences,
    type WorkspaceLanguage as Language,
    type WorkspacePreferences as PreferencesState,
    type WorkspaceTheme as Theme,
} from "@shared/theme/workspacePreferences";

const languageOptions: Array<{value: Language; label: string}> = [
    {value: "en", label: "English (EN)"},
    {value: "ru", label: "Русский (RU)"},
];

const themeOptions: Array<{
    value: Theme;
    label: string;
    availability: string;
    description: string;
}> = [
    {
        value: "matrix",
        label: "Matrix",
        availability: "Animated signature",
        description: "Our live green control-panel theme with the animated Matrix shell.",
    },
    {
        value: "dark",
        label: "Dark",
        availability: "Classic static",
        description: "A plain dark interface without animated overlays or stylized motion.",
    },
    {
        value: "light",
        label: "Light",
        availability: "Classic static",
        description: "A plain light interface for regular daytime work and neutral dashboards.",
    },
];

const PreferencesCard = () => {
    const {
        preferences: storedPreferences,
        savePreferences,
    } = useWorkspacePreferences();

    const [preferences, setPreferences] = useState<PreferencesState>(storedPreferences);
    const [savedPreferences, setSavedPreferences] = useState<PreferencesState>(
        storedPreferences,
    );
    const [isSavingPreferences, setIsSavingPreferences] = useState(false);
    const [preferencesSaved, setPreferencesSaved] = useState(false);

    const activeTheme =
        themeOptions.find(({value}) => value === storedPreferences.theme) ?? themeOptions[0];
    const activeLanguage =
        languageOptions.find(({value}) => value === storedPreferences.language) ??
        languageOptions[0];

    const selectedTheme =
        themeOptions.find(({value}) => value === preferences.theme) ?? themeOptions[0];

    const isDirty =
        preferences.language !== savedPreferences.language ||
        preferences.theme !== savedPreferences.theme;

    useEffect(() => {
        if (!preferencesSaved) {
            return undefined;
        }

        const timeoutId = window.setTimeout(() => {
            setPreferencesSaved(false);
        }, 2200);

        return () => {
            window.clearTimeout(timeoutId);
        };
    }, [preferencesSaved]);

    useEffect(() => {
        setPreferences(storedPreferences);
        setSavedPreferences(storedPreferences);
    }, [storedPreferences]);

    const applyPreferences = (nextPreferences: PreferencesState) => {
        setIsSavingPreferences(true);
        setPreferencesSaved(false);

        window.setTimeout(() => {
            savePreferences(nextPreferences);
            setSavedPreferences(nextPreferences);
            setIsSavingPreferences(false);
            setPreferencesSaved(true);
        }, 320);
    };

    const handlePreferencesSubmit = (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        if (!isDirty) {
            return;
        }

        applyPreferences(preferences);
    };

    return (
        <section className="settings-card settings-card--preferences">
            <div className="settings-card-header">
                <div>
                    <h2 className="settings-card-title">App profile</h2>
                    <p className="settings-card-description">
                        Choose how this application should look during your
                        sessions.
                    </p>
                </div>
            </div>

            <div className="preferences-spotlight">
                <div className="preferences-spotlight__eyebrow">Current setup</div>
                <div className="preferences-spotlight__headline">
                    <div>
                        <h3 className="preferences-spotlight__title">
                            {activeTheme.label}
                        </h3>
                        <p className="preferences-spotlight__description">
                            {activeTheme.description}
                        </p>
                    </div>
                    <span className="settings-pill">{activeTheme.availability}</span>
                </div>

                <div className="preferences-spotlight__meta">
                    <span className="preferences-spotlight__meta-item">
                        Interface: {activeLanguage.label}
                    </span>
                    <span className="preferences-spotlight__meta-item">
                        Scope: This device
                    </span>
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
                        value={preferences.language}
                        onChange={(e) =>
                            setPreferences((current) => ({
                                ...current,
                                language: e.target.value as Language,
                            }))
                        }
                    >
                        {languageOptions.map((option) => (
                            <option key={option.value} value={option.value}>
                                {option.label}
                            </option>
                        ))}
                    </select>
                </div>

                <div className="settings-field">
                    <div className="settings-label-row">
                        <label className="settings-label" htmlFor="theme">
                            Theme preset
                        </label>
                        <span>{selectedTheme.availability}</span>
                    </div>

                    <select
                        id="theme"
                        className="settings-input settings-select"
                        value={preferences.theme}
                        onChange={(e) =>
                            setPreferences((current) => ({
                                ...current,
                                theme: e.target.value as Theme,
                            }))
                        }
                    >
                        {themeOptions.map((option) => (
                            <option key={option.value} value={option.value}>
                                {option.label}
                            </option>
                        ))}
                    </select>

                    <p className="settings-hint">
                        Matrix keeps the animated shell. Dark and Light stay deliberately
                        plain and static, like classic application themes.
                    </p>
                </div>

                <div className="preferences-actions">
                    <div className="preferences-actions__copy">
                        <div className="preferences-actions__eyebrow">
                            Workspace defaults
                        </div>
                        <p className="preferences-actions__text">
                            Save the draft locally so this app opens with your preferred
                            language and theme preset next time.
                        </p>
                    </div>

                    <div className="settings-actions-row settings-actions-row--preferences">
                        {preferencesSaved && (
                            <span className="settings-save-badge">Saved locally</span>
                        )}

                        <button
                            type="submit"
                            className="settings-button settings-button--preferences"
                            disabled={isSavingPreferences || !isDirty}
                        >
                            {isSavingPreferences ? (
                                <>
                                    <LoaderCircle className="settings-button__icon settings-button__icon--spin"/>
                                    Applying...
                                </>
                            ) : (
                                <>
                                    <Sparkles className="settings-button__icon"/>
                                    {isDirty ? "Apply workspace setup" : "Workspace ready"}
                                </>
                            )}
                        </button>
                    </div>
                </div>
            </form>
        </section>
    );
};

export default PreferencesCard;
