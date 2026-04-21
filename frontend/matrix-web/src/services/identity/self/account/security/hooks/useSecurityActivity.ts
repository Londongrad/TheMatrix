import {useState} from "react";
import {fetchSecurityActivityFeed} from "@services/identity/api/self/account/accountApi";
import {useCursorFeed} from "@shared/lib/paging/useCursorFeed";

const DEFAULT_PAGE_SIZE = 12;

export function useSecurityActivity(
    token: string | null,
    options?: {
        enabled?: boolean;
    },
) {
    const enabled = options?.enabled ?? true;
    const [refreshVersion, setRefreshVersion] = useState(0);
    const feed = useCursorFeed(
        (cursor, pageSize, signal) => fetchSecurityActivityFeed(cursor, pageSize, signal),
        [token, refreshVersion],
        {
            enabled: enabled && Boolean(token),
            pageSize: DEFAULT_PAGE_SIZE,
            errorMessage: "Failed to load security activity.",
        },
    );

    return {
        items: feed.items,
        hasNext: feed.hasNext,
        isLoadingInitial: feed.isLoadingInitial,
        isLoadingMore: feed.isLoadingMore,
        error: feed.error,
        reload: () => {
            setRefreshVersion((value) => value + 1);
        },
        loadMore: feed.loadMore,
    };
}
