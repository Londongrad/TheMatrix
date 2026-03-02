import {useEffect, useState} from "react";
import {fetchSecurityActivity} from "@services/identity/api/self/account/accountApi";
import type {SecurityActivityItem} from "@services/identity/api/self/account/accountTypes";

const DEFAULT_LIMIT = 12;

export function useSecurityActivity(
    token: string | null,
    options?: {
        enabled?: boolean;
    },
) {
    const enabled = options?.enabled ?? true;
    const [items, setItems] = useState<SecurityActivityItem[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [hasLoaded, setHasLoaded] = useState(false);

    const load = async () => {
        if (!token) {
            setItems([]);
            setError(null);
            setIsLoading(false);
            setHasLoaded(false);
            return;
        }

        try {
            setIsLoading(true);
            setError(null);
            const response = await fetchSecurityActivity(DEFAULT_LIMIT);
            setItems(response);
            setHasLoaded(true);
        } catch (err: any) {
            console.error(err);
            setError(err?.message || "Failed to load security activity.");
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        if (!token) {
            setItems([]);
            setError(null);
            setIsLoading(false);
            setHasLoaded(false);
        }
    }, [token]);

    useEffect(() => {
        if (enabled && token && !hasLoaded) {
            void load();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [enabled, token, hasLoaded]);

    return {
        items,
        isLoading,
        error,
        hasLoaded,
        reload: load,
    };
}
