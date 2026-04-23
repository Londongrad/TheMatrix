import {createContext, useContext} from "react";

export type WorkspaceLanguage = "en" | "ru";
export type WorkspaceTheme = "matrix" | "dark" | "light";

export type WorkspacePreferences = {
    language: WorkspaceLanguage;
    theme: WorkspaceTheme;
    animateSidebarBackButton: boolean;
};

export type WorkspacePreferencesContextValue = {
    preferences: WorkspacePreferences;
    savePreferences: (nextPreferences: WorkspacePreferences) => void;
};

const STORAGE_KEY = "matrix.identity.preferences";

export const defaultWorkspacePreferences: WorkspacePreferences = {
    language: "en",
    theme: "matrix",
    animateSidebarBackButton: true,
};

export const WorkspacePreferencesContext =
    createContext<WorkspacePreferencesContextValue | null>(null);

export function readStoredWorkspacePreferences(): WorkspacePreferences {
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
                parsed.theme === "matrix"
                    ? parsed.theme
                    : defaultWorkspacePreferences.theme,
            animateSidebarBackButton:
                typeof parsed.animateSidebarBackButton === "boolean"
                    ? parsed.animateSidebarBackButton
                    : defaultWorkspacePreferences.animateSidebarBackButton,
        };
    } catch {
        return defaultWorkspacePreferences;
    }
}

export function persistWorkspacePreferences(preferences: WorkspacePreferences) {
    if (typeof window === "undefined") {
        return;
    }

    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(preferences));
}

export function applyDocumentPreferences(preferences: WorkspacePreferences) {
    if (typeof document === "undefined") {
        return;
    }

    document.documentElement.dataset.theme = preferences.theme;
    document.body.dataset.theme = preferences.theme;
    document.documentElement.lang = preferences.language;
    document.documentElement.style.colorScheme =
        preferences.theme === "light" ? "light" : "dark";
}

export function useWorkspacePreferences() {
    const context = useContext(WorkspacePreferencesContext);

    if (!context) {
        throw new Error(
            "useWorkspacePreferences must be used within WorkspacePreferencesProvider.",
        );
    }

    return context;
}
