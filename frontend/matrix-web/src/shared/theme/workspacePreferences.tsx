import {type ReactNode, useEffect, useMemo, useState,} from "react";
import {
    applyDocumentPreferences,
    persistWorkspacePreferences,
    readStoredWorkspacePreferences,
    WORKSPACE_PREFERENCES_STORAGE_KEY,
    type WorkspacePreferences,
    WorkspacePreferencesContext,
    type WorkspacePreferencesContextValue,
} from "./workspacePreferencesContext";

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
            if (event.key !== WORKSPACE_PREFERENCES_STORAGE_KEY) {
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
