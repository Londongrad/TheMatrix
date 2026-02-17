import {
    createContext,
    useContext,
    useEffect,
    useMemo,
    useState,
    type ReactNode,
} from "react";

export type WorkspaceLanguage = "en" | "ru";
export type WorkspaceTheme = "matrix" | "dark" | "light" | "glass";

export type WorkspacePreferences = {
    language: WorkspaceLanguage;
    theme: WorkspaceTheme;
};

type WorkspacePreferencesContextValue = {
    preferences: WorkspacePreferences;
    savePreferences: (nextPreferences: WorkspacePreferences) => void;
};

const STORAGE_KEY = "matrix.identity.preferences";

export const defaultWorkspacePreferences: WorkspacePreferences = {
    language: "en",
    theme: "matrix",
};

const WorkspacePreferencesContext =
    createContext<WorkspacePreferencesContextValue | null>(null);

export const readStoredWorkspacePreferences = (): WorkspacePreferences => {
    if (typeof window === "undefined") {
        return defaultWorkspacePreferences;
    }

    try {
        const raw = window.localStorage.getItem(STORAGE_KEY);

        if (!raw) {
            return defaultWorkspacePreferences;
        }

        const parsed = JSON.parse(raw) as Partial<WorkspacePreferences>;

        return {
            language: parsed.language === "ru" ? "ru" : "en",
            theme:
                parsed.theme === "dark" ||
                parsed.theme === "light" ||
                parsed.theme === "glass" ||
                parsed.theme === "matrix"
                    ? parsed.theme
                    : defaultWorkspacePreferences.theme,
        };
    } catch {
        return defaultWorkspacePreferences;
    }
};

const persistWorkspacePreferences = (preferences: WorkspacePreferences) => {
    if (typeof window === "undefined") {
        return;
    }

    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(preferences));
};

const applyDocumentPreferences = (preferences: WorkspacePreferences) => {
    if (typeof document === "undefined") {
        return;
    }

    document.documentElement.dataset.theme = preferences.theme;
    document.body.dataset.theme = preferences.theme;
    document.documentElement.lang = preferences.language;
    document.documentElement.style.colorScheme =
        preferences.theme === "light" ? "light" : "dark";
};

export function WorkspacePreferencesProvider({
    children,
}: {
    children: ReactNode;
}) {
    const [preferences, setPreferences] = useState<WorkspacePreferences>(() =>
        readStoredWorkspacePreferences(),
    );

    useEffect(() => {
        applyDocumentPreferences(preferences);
    }, [preferences]);

    useEffect(() => {
        if (typeof window === "undefined") {
            return undefined;
        }

        const syncPreferences = (event: StorageEvent) => {
            if (event.key !== STORAGE_KEY) {
                return;
            }

            setPreferences(readStoredWorkspacePreferences());
        };

        window.addEventListener("storage", syncPreferences);

        return () => {
            window.removeEventListener("storage", syncPreferences);
        };
    }, []);

    const value = useMemo<WorkspacePreferencesContextValue>(
        () => ({
            preferences,
            savePreferences: (nextPreferences) => {
                persistWorkspacePreferences(nextPreferences);
                setPreferences(nextPreferences);
            },
        }),
        [preferences],
    );

    return (
        <WorkspacePreferencesContext.Provider value={value}>
            {children}
        </WorkspacePreferencesContext.Provider>
    );
}

export const useWorkspacePreferences = () => {
    const context = useContext(WorkspacePreferencesContext);

    if (!context) {
        throw new Error(
            "useWorkspacePreferences must be used within WorkspacePreferencesProvider.",
        );
    }

    return context;
};
