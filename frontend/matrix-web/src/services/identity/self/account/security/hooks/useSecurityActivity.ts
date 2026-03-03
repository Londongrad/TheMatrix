import {useState} from "react";
import {fetchSecurityActivityPage} from "@services/identity/api/self/account/accountApi";
import {usePagedQuery} from "@shared/lib/paging/usePagedQuery";

const DEFAULT_PAGE_SIZE = 12;

export function useSecurityActivity(
    token: string | null,
    options?: {
        enabled?: boolean;
    },
) {
    const enabled = options?.enabled ?? true;
    const [refreshVersion, setRefreshVersion] = useState(0);
    const query = usePagedQuery(
        fetchSecurityActivityPage,
        DEFAULT_PAGE_SIZE,
        [token, refreshVersion],
        {
            enabled: enabled && Boolean(token),
            initialPage: 1,
            errorMessage: "Failed to load security activity.",
        },
    );

    return {
        items: query.data?.items ?? [],
        totalCount: query.data?.totalCount ?? 0,
        totalPages: query.data?.totalPages ?? 1,
        pageNumber: query.data?.pageNumber ?? query.pageNumber,
        pageSize: query.data?.pageSize ?? DEFAULT_PAGE_SIZE,
        hasLoaded: query.data !== null,
        isLoading: query.isLoading,
        error: query.error,
        reload: () => {
            setRefreshVersion((value) => value + 1);
        },
        setPageNumber: query.setPageNumber,
    };
}
