import {Sparkles, LoaderCircle} from "lucide-react";
import {useEffect, useRef, useState, type FormEvent} from "react";
import "@services/identity/self/account/preferences/styles/preferences-card.css";

type Language = "en" | "ru";
type Theme = "matrix" | "dark" | "light" | "glass";

type PreferencesState = {
    language: Language;
    theme: Theme;
};

const PREFERENCES_STORAGE_KEY = "matrix.identity.preferences";

const languageOptions: Array<{value: Language; label: string}> = [
    {value: "en", label: "English (EN)"},
    {value: "ru", label: "Russian (RU)"},
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
        availability: "Live default",
        description: "The current neon control-panel theme used across the app today.",
    },
    {
        value: "dark",
        label: "Dark",
        availability: "Preview stub",
        description: "A softer dark shell for long operator sessions and future theme rollout.",
    },
    {
        value: "light",
        label: "Light",
        availability: "Preview stub",
        description: "A brighter utility palette for daylight dashboards and admin work.",
    },
    {
        value: "glass",
        label: "Glass",
        availability: "Preview stub",
        description: "A translucent concept theme reserved for future visual experiments.",
    },
];

const defaultPreferences: PreferencesState = {
    language: "en",
    theme: "matrix",
};

const readStoredPreferences = (): PreferencesState => {
    if (typeof window === "undefined") {
        return defaultPreferences;
    }

    try {
        const raw = window.localStorage.getItem(PREFERENCES_STORAGE_KEY);

        if (!raw) {
            return defaultPreferences;
        }

        const parsed = JSON.parse(raw) as Partial<PreferencesState>;

        return {
            language: languageOptions.some(({value}) => value === parsed.language)
                ? parsed.language!
                : defaultPreferences.language,
            theme: themeOptions.some(({value}) => value === parsed.theme)
                ? parsed.theme!
                : defaultPreferences.theme,
        };
    } catch {
        return defaultPreferences;
    }
};

const persistPreferences = (preferences: PreferencesState) => {
    if (typeof window === "undefined") {
        return;
    }

    window.localStorage.setItem(
        PREFERENCES_STORAGE_KEY,
        JSON.stringify(preferences),
    );
};

const PreferencesCard = () => {
    const initialPreferencesRef = useRef<PreferencesState>(readStoredPreferences());

    const [preferences, setPreferences] = useState<PreferencesState>(
        initialPreferencesRef.current,
    );
    const [savedPreferences, setSavedPreferences] = useState<PreferencesState>(
        initialPreferencesRef.current,
    );
    const [isSavingPreferences, setIsSavingPreferences] = useState(false);
    const [preferencesSaved, setPreferencesSaved] = useState(false);

    const selectedTheme =
        themeOptions.find(({value}) => value === preferences.theme) ?? themeOptions[0];
    const selectedLanguage =
        languageOptions.find(({value}) => value === preferences.language) ??
        languageOptions[0];

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

    const applyPreferences = (nextPreferences: PreferencesState) => {
        setIsSavingPreferences(true);
        setPreferencesSaved(false);

        window.setTimeout(() => {
            persistPreferences(nextPreferences);
            setSavedPreferences(nextPreferences);
            setIsSavingPreferences(false);
            setPreferencesSaved(true);
        }, 420);
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
                    <h2 className="settings-card-title">Console profile</h2>
                    <p className="settings-card-description">
                        Choose how this workstation should look and speak during operator
                        sessions.
                    </p>
                </div>
            </div>

            <div className="preferences-spotlight">
                <div className="preferences-spotlight__eyebrow">Current setup</div>
                <div className="preferences-spotlight__headline">
                    <div>
                        <h3 className="preferences-spotlight__title">
                            {selectedTheme.label}
                        </h3>
                        <p className="preferences-spotlight__description">
                            {selectedTheme.description}
                        </p>
                    </div>
                    <span className="settings-pill">{selectedTheme.availability}</span>
                </div>

                <div className="preferences-spotlight__meta">
                    <span className="preferences-spotlight__meta-item">
                        Interface: {selectedLanguage.label}
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

                    <div className="preferences-theme-note">
                        <strong>{selectedTheme.label}</strong>
                        <span>{selectedTheme.description}</span>
                    </div>

                    <p className="settings-hint">
                        Only Matrix is wired into the live shell right now. The other
                        presets are intentionally staged as future theme placeholders.
                    </p>
                </div>

                <div className="preferences-actions">
                    <div className="preferences-actions__copy">
                        <div className="preferences-actions__eyebrow">
                            Workspace defaults
                        </div>
                        <p className="preferences-actions__text">
                            Save the draft locally so this console opens with your preferred
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
