import {useEffect, useState} from "react";
import type {CursorPagedResult} from "@shared/lib/paging/cursorPagingTypes";

type FetchSlice<T> = (
    cursor: string | null,
    pageSize: number,
    signal?: AbortSignal
) => Promise<CursorPagedResult<T>>;

interface UseCursorFeedOptions {
    enabled?: boolean;
    pageSize?: number;
    errorMessage?: string;
}

interface CursorFeedState<T> {
    items: T[];
    nextCursor: string | null;
    hasNext: boolean;
}

const DEFAULT_PAGE_SIZE = 50;

export function useCursorFeed<T>(
    fetchSlice: FetchSlice<T>,
    deps: unknown[] = [],
    options: UseCursorFeedOptions = {},
) {
    const {
        enabled = true,
        pageSize = DEFAULT_PAGE_SIZE,
        errorMessage = "Failed to load data.",
    } = options;

    const [state, setState] = useState<CursorFeedState<T>>({
        items: [],
        nextCursor: null,
        hasNext: false,
    });
    const [isLoadingInitial, setIsLoadingInitial] = useState(false);
    const [isLoadingMore, setIsLoadingMore] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [reloadToken, setReloadToken] = useState(0);

    useEffect(() => {
        if (!enabled) {
            setState({
                items: [],
                nextCursor: null,
                hasNext: false,
            });
            setError(null);
            setIsLoadingInitial(false);
            setIsLoadingMore(false);
            return;
        }

        const abortController = new AbortController();
        let isActual = true;

        (async () => {
            try {
                setIsLoadingInitial(true);
                setError(null);
                setState({
                    items: [],
                    nextCursor: null,
                    hasNext: false,
                });

                const result = await fetchSlice(
                    null,
                    pageSize,
                    abortController.signal,
                );

                if (!isActual) {
                    return;
                }

                setState({
                    items: result.items,
                    nextCursor: result.nextCursor,
                    hasNext: result.hasNext,
                });
            } catch (requestError) {
                if (!isActual || abortController.signal.aborted) {
                    return;
                }

                console.error(requestError);
                setState({
                    items: [],
                    nextCursor: null,
                    hasNext: false,
                });
                setError(errorMessage);
            } finally {
                if (!isActual) {
                    return;
                }

                setIsLoadingInitial(false);
                setIsLoadingMore(false);
            }
        })();

        return () => {
            isActual = false;
            abortController.abort();
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [enabled, pageSize, reloadToken, ...deps]);

    const loadMore = async () => {
        if (!enabled || isLoadingInitial || isLoadingMore || !state.hasNext || !state.nextCursor) {
            return;
        }

        try {
            setIsLoadingMore(true);
            setError(null);

            const result = await fetchSlice(
                state.nextCursor,
                pageSize,
            );

            setState((currentState) => ({
                items: [...currentState.items, ...result.items],
                nextCursor: result.nextCursor,
                hasNext: result.hasNext,
            }));
        } catch (requestError) {
            console.error(requestError);
            setError(errorMessage);
        } finally {
            setIsLoadingMore(false);
        }
    };

    const reset = () => {
        setReloadToken((value) => value + 1);
    };

    return {
        items: state.items,
        nextCursor: state.nextCursor,
        hasNext: state.hasNext,
        isLoadingInitial,
        isLoadingMore,
        error,
        loadMore,
        reset,
    };
}
