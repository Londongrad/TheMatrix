import {useEffect, useState} from "react";
import {fetchSecurityActivity} from "@services/identity/api/self/account/accountApi";
import type {SecurityActivityItem} from "@services/identity/api/self/account/accountTypes";

const DEFAULT_LIMIT = 12;

export function useSecurityActivity(token: string | null) {
    const [items, setItems] = useState<SecurityActivityItem[]>([]);
    const [isLoading, setIsLoading] = useState(Boolean(token));
    const [error, setError] = useState<string | null>(null);

    const load = async () => {
        if (!token) {
            setItems([]);
            setError(null);
            setIsLoading(false);
            return;
        }

        try {
            setIsLoading(true);
            setError(null);
            const response = await fetchSecurityActivity(DEFAULT_LIMIT);
            setItems(response);
        } catch (err: any) {
            console.error(err);
            setError(err?.message || "Failed to load security activity.");
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        void load();
    }, [token]);

    return {
        items,
        isLoading,
        error,
        reload: load,
    };
}
