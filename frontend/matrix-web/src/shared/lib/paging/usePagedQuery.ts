// src/shared/lib/paging/usePagedQuery.ts
import { useEffect, useState } from "react";
import type { PagedResult } from "@shared/lib/paging/pagingTypes";

type FetchPage<T> = (
  pageNumber: number,
  pageSize: number
) => Promise<PagedResult<T>>;

interface UsePagedQueryOptions {
  enabled?: boolean;
  initialPage?: number;
  keepPreviousData?: boolean;
  errorMessage?: string;
}

export function usePagedQuery<T>(
  fetchPage: FetchPage<T>,
  pageSize: number,
  deps: unknown[] = [],
  options: UsePagedQueryOptions = {}
) {
  const {
    enabled = true,
    initialPage = 1,
    keepPreviousData = true,
    errorMessage = "Failed to load data.",
  } = options;

  const [pageNumber, setPageNumber] = useState(initialPage);
  const [data, setData] = useState<PagedResult<T> | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!enabled) {
      setData(null);
      setPageNumber(initialPage);
      setError(null);
      setIsLoading(false);
      return;
    }

    let isActual = true;

    (async () => {
      try {
        setIsLoading(true);
        setError(null);

        const result = await fetchPage(pageNumber, pageSize);

        if (!isActual) return;
        setData(result);

        // страховка: если сервер вернул меньше страниц, чем текущая
        if (result.totalPages > 0 && pageNumber > result.totalPages) {
          setPageNumber(result.totalPages);
        }
      } catch (e) {
        if (!isActual) return;
        console.error(e);
        setError(errorMessage);

        if (!keepPreviousData) setData(null);
      } finally {
        if (!isActual) return;
        setIsLoading(false);
      }
    })();

    return () => {
      isActual = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [enabled, pageNumber, pageSize, ...deps]);

  const reset = () => {
    setData(null);
    setPageNumber(initialPage);
    setError(null);
    setIsLoading(false);
  };

  return { data, pageNumber, setPageNumber, isLoading, error, reset };
}
